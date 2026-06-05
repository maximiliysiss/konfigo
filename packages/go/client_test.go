package konfigo

import (
	"context"
	"io"
	"testing"
	"time"
)

type changedOptions struct {
	Group `konfigo:"key=ChangedOptions"`
	Value int `konfigo:"key,default=42"`
}

type fakeTransport struct {
	created   *CreateVersionRequest
	versionID *string
	events    chan SubscriptionEvent
	initial   []ConfigEntry
}

func (f *fakeTransport) IsVersionExists(context.Context, IsVersionExistRequest) (IsVersionExistResponse, error) {
	return IsVersionExistResponse{VersionID: f.versionID}, nil
}

func (f *fakeTransport) CreateVersion(_ context.Context, request CreateVersionRequest) (CreateVersionResponse, error) {
	f.created = &request
	return CreateVersionResponse{VersionID: "version-1"}, nil
}

func (f *fakeTransport) StartSubscribe(context.Context, StartSubscribeRequest) (SubscriptionStream, error) {
	return &fakeStream{events: f.events}, nil
}

func (f *fakeTransport) GetConfig(context.Context, string, string) ([]ConfigEntry, error) {
	return f.initial, nil
}

type fakeStream struct {
	events chan SubscriptionEvent
}

func (s *fakeStream) Recv() (SubscriptionEvent, error) {
	event, ok := <-s.events
	if !ok {
		return SubscriptionEvent{}, io.EOF
	}
	return event, nil
}

func TestEnsureVersionCreatesWhenMissing(t *testing.T) {
	definitions, err := DiscoverDefinitions(changedOptions{})
	if err != nil {
		t.Fatal(err)
	}
	transport := &fakeTransport{events: make(chan SubscriptionEvent)}
	client := NewClient(
		RealtimeConfigOptions{IsEnabled: true, ServiceID: "orders", Version: "1.0.0"},
		transport,
		definitions,
	)

	version, err := client.EnsureVersion(context.Background())
	if err != nil {
		t.Fatal(err)
	}
	if version.Value != "version-1" {
		t.Fatalf("unexpected version %q", version.Value)
	}
	if transport.created == nil || transport.created.ServiceID != "orders" {
		t.Fatalf("unexpected create request %#v", transport.created)
	}
}

func TestWatchFiltersInitialGenerationAndAppliesUpdates(t *testing.T) {
	definitions, err := DiscoverDefinitions(changedOptions{})
	if err != nil {
		t.Fatal(err)
	}
	transport := &fakeTransport{events: make(chan SubscriptionEvent, 1)}
	client := NewClient(
		RealtimeConfigOptions{IsEnabled: true, ServiceID: "orders", Version: "1.0.0", PollingInterval: time.Millisecond},
		transport,
		definitions,
	)

	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()
	errs := make(chan error, 1)
	go func() {
		errs <- client.Watch(ctx)
	}()

	transport.events <- SubscriptionEvent{Events: []ConfigEntry{
		{Key: "ChangedOptions:Value", Value: ptr("43"), Generation: 1, Timestamp: time.Now()},
		{Key: "ChangedOptions:Value", Value: ptr("44"), Generation: 2, Timestamp: time.Now()},
	}}

	deadline := time.After(time.Second)
	for {
		if value, ok := client.Store.Get("ChangedOptions:Value"); ok && value != nil && *value == "44" {
			cancel()
			return
		}
		select {
		case err := <-errs:
			t.Fatalf("watch exited early: %v", err)
		case <-deadline:
			t.Fatal("timed out waiting for update")
		default:
			time.Sleep(time.Millisecond)
		}
	}
}

func TestNewClientFromRemoteLoadsInitialEntries(t *testing.T) {
	definitions, err := DiscoverDefinitions(changedOptions{})
	if err != nil {
		t.Fatal(err)
	}
	transport := &fakeTransport{
		events: make(chan SubscriptionEvent),
		initial: []ConfigEntry{
			{Key: "ChangedOptions:Value", Value: ptr("42"), Generation: 1, Timestamp: time.Now()},
		},
	}

	client, err := NewClientFromRemote(
		context.Background(),
		RealtimeConfigOptions{IsEnabled: true, ServiceID: "orders", Version: "1.0.0"},
		transport,
		definitions,
	)
	if err != nil {
		t.Fatal(err)
	}

	value, ok := client.Store.Get("ChangedOptions:Value")
	if !ok || value == nil || *value != "42" {
		t.Fatalf("unexpected initial value %#v", value)
	}
}
