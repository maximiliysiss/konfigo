package konfigo

import "context"

type Transport interface {
	IsVersionExists(ctx context.Context, request IsVersionExistRequest) (IsVersionExistResponse, error)
	CreateVersion(ctx context.Context, request CreateVersionRequest) (CreateVersionResponse, error)
	StartSubscribe(ctx context.Context, request StartSubscribeRequest) (SubscriptionStream, error)
}

type InitialConfigTransport interface {
	Transport
	GetConfig(ctx context.Context, serviceID, version string) ([]ConfigEntry, error)
}

type SubscriptionStream interface {
	Recv() (SubscriptionEvent, error)
}
