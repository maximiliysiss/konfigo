package main

import (
	"context"
	"encoding/json"
	"errors"
	"log"
	"net/http"
	"os"
	"os/signal"
	"time"

	konfigo "github.com/maximiliysiss/konfigo/packages/go"
	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
)

type PaymentsOptions struct {
	konfigo.Group `konfigo:"key=Payments,name=Payments,description=Payment gateway"`

	Provider       string        `konfigo:"key,name=Provider,description=Payment provider,default=Stripe"`
	Timeout        time.Duration `konfigo:"key,name=Timeout,description=Provider request timeout,default=00:00:30"`
	RetryCount     int           `konfigo:"key,name=Retry count,description=Retry attempts before failing,default=3"`
	EnableFallback bool          `konfigo:"key,name=Enable fallback,description=Use fallback provider,default=true"`
}

func main() {
	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt)
	defer stop()

	grpcURL := env("KONFIGO_GRPC_URL", "localhost:8081")
	serviceID := env("KONFIGO_SERVICE_ID", "f89f7a09-d71d-459d-b02c-07213ed0eaa4")
	version := env("KONFIGO_VERSION", "1.0.13")
	httpAddr := env("HTTP_ADDR", ":8088")

	definitions, err := konfigo.DiscoverDefinitions(PaymentsOptions{})
	if err != nil {
		log.Fatalf("discover definitions: %v", err)
	}

	conn, err := grpc.NewClient(grpcURL, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		log.Fatalf("connect to Konfigo gRPC: %v", err)
	}
	defer conn.Close()

	transport := konfigo.NewGrpcTransport(conn)
	options := konfigo.RealtimeConfigOptions{
		IsEnabled: true,
		ServiceID: serviceID,
		Version:   version,
		URL:       grpcURL,
	}

	client := konfigo.NewClient(options, transport, definitions)
	if _, err := client.EnsureVersion(ctx); err != nil {
		log.Fatalf("ensure config version: %v", err)
	}

	entries, err := transport.GetConfig(ctx, serviceID, version)
	if err != nil {
		log.Fatalf("load config snapshot: %v", err)
	}
	client.Store.Update(entries, false)

	go func() {
		if err := client.Watch(ctx); err != nil && !errors.Is(err, context.Canceled) {
			log.Printf("konfigo watch stopped: %v", err)
		}
	}()

	mux := http.NewServeMux()
	mux.HandleFunc("/options", func(w http.ResponseWriter, r *http.Request) {
		var payments PaymentsOptions
		if err := konfigo.BindConfig(client.Store.Snapshot(), &payments); err != nil {
			http.Error(w, err.Error(), http.StatusInternalServerError)
			return
		}

		w.Header().Set("Content-Type", "application/json")
		if err := json.NewEncoder(w).Encode(payments); err != nil {
			log.Printf("write response: %v", err)
		}
	})

	server := &http.Server{
		Addr:              httpAddr,
		Handler:           mux,
		ReadHeaderTimeout: 5 * time.Second,
	}

	go func() {
		<-ctx.Done()
		shutdownCtx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
		defer cancel()
		if err := server.Shutdown(shutdownCtx); err != nil {
			log.Printf("http shutdown: %v", err)
		}
	}()

	log.Printf("Go sample listening on http://localhost%s/options", httpAddr)
	if err := server.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
		log.Fatalf("http server: %v", err)
	}
}

func env(key, fallback string) string {
	value := os.Getenv(key)
	if value == "" {
		return fallback
	}
	return value
}
