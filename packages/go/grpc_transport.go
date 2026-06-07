package konfigo

import (
	"context"

	"github.com/maximiliysiss/konfigo/packages/go/internal/proto"
	"google.golang.org/grpc"
	"google.golang.org/protobuf/types/known/timestamppb"
	"google.golang.org/protobuf/types/known/wrapperspb"
)

type GrpcTransport struct {
	client proto.RealtimeConfigGrpcServiceClient
}

func NewGrpcTransport(conn grpc.ClientConnInterface) *GrpcTransport {
	return &GrpcTransport{client: proto.NewRealtimeConfigGrpcServiceClient(conn)}
}

func (t *GrpcTransport) IsVersionExists(ctx context.Context, request IsVersionExistRequest) (IsVersionExistResponse, error) {
	response, err := t.client.IsVersionExists(ctx, &proto.IsVersionExistRequest{
		ServiceId: request.ServiceID,
		Version:   request.Version,
	})
	if err != nil {
		return IsVersionExistResponse{}, err
	}

	var versionID *string
	if response.GetVersionId() != nil {
		value := response.GetVersionId().GetValue()
		versionID = &value
	}

	return IsVersionExistResponse{VersionID: versionID}, nil
}

func (t *GrpcTransport) CreateVersion(ctx context.Context, request CreateVersionRequest) (CreateVersionResponse, error) {
	response, err := t.client.CreateVersion(ctx, mapCreateVersionRequest(request))
	if err != nil {
		return CreateVersionResponse{}, err
	}
	return CreateVersionResponse{VersionID: response.GetVersionId()}, nil
}

func (t *GrpcTransport) StartSubscribe(ctx context.Context, request StartSubscribeRequest) (SubscriptionStream, error) {
	stream, err := t.client.StartSubscribe(ctx, &proto.StartSubscribeRequest{
		ServiceId: request.ServiceID,
		VersionId: request.VersionID,
		Timestamp: timestamppb.New(normalizeTime(request.Timestamp)),
	})
	if err != nil {
		return nil, err
	}
	return &grpcSubscriptionStream{stream: stream}, nil
}

func (t *GrpcTransport) GetConfig(ctx context.Context, serviceID, version string) ([]ConfigEntry, error) {
	response, err := t.client.GetConfig(ctx, &proto.GetConfigRequest{
		ServiceId: serviceID,
		Version:   version,
	})
	if err != nil {
		return nil, err
	}

	entries := make([]ConfigEntry, 0, len(response.GetEntries()))
	for _, entry := range response.GetEntries() {
		entries = append(entries, ConfigEntry{
			Key:        entry.GetKey(),
			Value:      unwrapString(entry.GetValue()),
			Type:       ValueType(entry.GetType()),
			Generation: entry.GetGeneration(),
			Timestamp:  entry.GetTimestamp().AsTime(),
		})
	}
	return entries, nil
}

type grpcSubscriptionStream struct {
	stream proto.RealtimeConfigGrpcService_StartSubscribeClient
}

func (s *grpcSubscriptionStream) Recv() (SubscriptionEvent, error) {
	event, err := s.stream.Recv()
	if err != nil {
		return SubscriptionEvent{}, err
	}

	entries := make([]ConfigEntry, 0, len(event.GetEvents()))
	for _, item := range event.GetEvents() {
		entries = append(entries, ConfigEntry{
			Key:        item.GetKey(),
			Value:      unwrapString(item.GetValue()),
			Type:       ValueType(item.GetType()),
			Generation: item.GetGeneration(),
			Timestamp:  item.GetTimestamp().AsTime(),
		})
	}

	return SubscriptionEvent{Events: entries}, nil
}

func mapCreateVersionRequest(request CreateVersionRequest) *proto.CreateVersionRequest {
	result := &proto.CreateVersionRequest{
		ServiceId: request.ServiceID,
		Version:   request.Version,
		Classes:   make([]*proto.CreateVersionRequest_ClassEntry, 0, len(request.Classes)),
	}

	for _, classDefinition := range request.Classes {
		classEntry := &proto.CreateVersionRequest_ClassEntry{
			Name:        classDefinition.Name,
			Description: wrapString(classDefinition.Description),
			Entries:     make([]*proto.CreateVersionRequest_ClassEntry_ConfigEntry, 0, len(classDefinition.Options)),
		}
		for _, option := range classDefinition.Options {
			classEntry.Entries = append(classEntry.Entries, &proto.CreateVersionRequest_ClassEntry_ConfigEntry{
				Key:         option.Key,
				Name:        option.Name,
				Description: wrapString(option.Description),
				ValueType:   proto.ValueType(option.Type),
				EnumValues:  wrapString(optionalJoin(option.EnumValues)),
				Value:       wrapString(option.DefaultValue),
			})
		}
		result.Classes = append(result.Classes, classEntry)
	}

	return result
}

func wrapString(value *string) *wrapperspb.StringValue {
	if value == nil {
		return nil
	}
	return wrapperspb.String(*value)
}

func unwrapString(value *wrapperspb.StringValue) *string {
	if value == nil {
		return nil
	}
	unwrapped := value.GetValue()
	return &unwrapped
}

func optionalJoin(values []string) *string {
	if values == nil {
		return nil
	}
	joined := ""
	for i, value := range values {
		if i > 0 {
			joined += ","
		}
		joined += value
	}
	return &joined
}
