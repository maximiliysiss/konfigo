package konfigo

import (
	"time"
)

type ValueType int32

const (
	ValueTypeUnknown  ValueType = 0
	ValueTypeString   ValueType = 1
	ValueTypeBoolean  ValueType = 2
	ValueTypeDateTime ValueType = 3
	ValueTypeTimeSpan ValueType = 4
	ValueTypeEnum     ValueType = 5
	ValueTypeNumber   ValueType = 6
	ValueTypeArray    ValueType = 7
	ValueTypeJSON     ValueType = 8
)

type ConfigEntry struct {
	Key        string
	Value      *string
	Type       ValueType
	Generation int32
	Timestamp  time.Time
}

type VersionID struct {
	Value string
}

type RealtimeConfigOptions struct {
	IsEnabled           bool
	ServiceID           string
	Version             string
	URL                 string
	Timestamp           time.Time
	PollingInterval     time.Duration
	InitialRequestDelay time.Duration
}

func (o RealtimeConfigOptions) WithDefaults() RealtimeConfigOptions {
	if o.PollingInterval == 0 {
		o.PollingInterval = 5 * time.Second
	}
	if o.InitialRequestDelay == 0 {
		o.InitialRequestDelay = 10 * time.Second
	}
	return o
}

type IsVersionExistRequest struct {
	ServiceID string
	Version   string
}

type IsVersionExistResponse struct {
	VersionID *string
}

type CreateVersionRequest struct {
	ServiceID string
	Version   string
	Classes   []ClassDefinition
}

type CreateVersionResponse struct {
	VersionID string
}

type StartSubscribeRequest struct {
	ServiceID string
	VersionID string
	Timestamp time.Time
}

type SubscriptionEvent struct {
	Events []ConfigEntry
}
