package konfigo

import (
	"context"
	"errors"
	"io"
	"time"
)

type Client struct {
	Options     RealtimeConfigOptions
	Transport   Transport
	Definitions []ClassDefinition
	Store       *Store
}

func NewClient(options RealtimeConfigOptions, transport Transport, definitions []ClassDefinition, initialEntries ...ConfigEntry) *Client {
	return &Client{
		Options:     options.WithDefaults(),
		Transport:   transport,
		Definitions: definitions,
		Store:       NewStore(initialEntries...),
	}
}

func NewClientFromRemote(ctx context.Context, options RealtimeConfigOptions, transport InitialConfigTransport, definitions []ClassDefinition) (*Client, error) {
	options = options.WithDefaults()
	entries, err := transport.GetConfig(ctx, options.ServiceID, options.Version)
	if err != nil {
		return nil, err
	}
	return NewClient(options, transport, definitions, entries...), nil
}

func (c *Client) EnsureVersion(ctx context.Context) (VersionID, error) {
	existing, err := c.Transport.IsVersionExists(ctx, IsVersionExistRequest{
		ServiceID: c.Options.ServiceID,
		Version:   c.Options.Version,
	})
	if err != nil {
		return VersionID{}, err
	}
	if existing.VersionID != nil {
		return VersionID{Value: *existing.VersionID}, nil
	}

	created, err := c.Transport.CreateVersion(ctx, CreateVersionRequest{
		ServiceID: c.Options.ServiceID,
		Version:   c.Options.Version,
		Classes:   c.Definitions,
	})
	if err != nil {
		return VersionID{}, err
	}

	return VersionID{Value: created.VersionID}, nil
}

func (c *Client) Watch(ctx context.Context) error {
	if !c.Options.IsEnabled || len(c.Definitions) == 0 {
		<-ctx.Done()
		return ctx.Err()
	}

	versionID, err := c.EnsureVersion(ctx)
	if err != nil {
		return err
	}

	timestamp := normalizeTime(c.Options.Timestamp)
	for {
		if err := ctx.Err(); err != nil {
			return err
		}

		stream, err := c.Transport.StartSubscribe(ctx, StartSubscribeRequest{
			ServiceID: c.Options.ServiceID,
			VersionID: versionID.Value,
			Timestamp: timestamp,
		})
		if err != nil {
			if waitErr := sleep(ctx, c.Options.PollingInterval); waitErr != nil {
				return waitErr
			}
			continue
		}

		for {
			event, err := stream.Recv()
			if err != nil {
				if errors.Is(err, context.Canceled) || errors.Is(err, context.DeadlineExceeded) {
					return err
				}
				if errors.Is(err, io.EOF) {
					break
				}
				if waitErr := sleep(ctx, c.Options.PollingInterval); waitErr != nil {
					return waitErr
				}
				break
			}

			updates := filterUpdates(event.Events)
			if len(updates) == 0 {
				continue
			}

			c.Store.Update(updates, true)
			for _, update := range updates {
				if ts := normalizeTime(update.Timestamp); ts.After(timestamp) {
					timestamp = ts
				}
			}
		}
	}
}

func filterUpdates(entries []ConfigEntry) []ConfigEntry {
	updates := make([]ConfigEntry, 0, len(entries))
	for _, entry := range entries {
		if entry.Generation > 1 {
			updates = append(updates, entry)
		}
	}
	return updates
}

func sleep(ctx context.Context, duration time.Duration) error {
	timer := time.NewTimer(duration)
	defer timer.Stop()

	select {
	case <-ctx.Done():
		return ctx.Err()
	case <-timer.C:
		return nil
	}
}
