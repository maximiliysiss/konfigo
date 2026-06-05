package konfigo

import (
	"testing"
	"time"
)

func TestStoreIgnoresStaleAndEqualGenerations(t *testing.T) {
	store := NewStore(ConfigEntry{
		Key:        "Options:Value",
		Value:      ptr("42"),
		Generation: 2,
		Timestamp:  time.Date(2026, 1, 1, 0, 0, 0, 0, time.UTC),
	})

	updated := store.Update([]ConfigEntry{
		{Key: "Options:Value", Value: ptr("41"), Generation: 1, Timestamp: time.Now()},
		{Key: "Options:Value", Value: ptr("42-again"), Generation: 2, Timestamp: time.Now()},
	}, true)

	if updated {
		t.Fatal("expected stale update to be ignored")
	}
	value, ok := store.Get("Options:Value")
	if !ok || value == nil || *value != "42" {
		t.Fatalf("unexpected value %#v", value)
	}
}

func TestStoreNotifiesSubscribersOnUpdate(t *testing.T) {
	store := NewStore()
	called := false
	unsubscribe := store.Subscribe(func(snapshot map[string]*string) {
		called = true
		value := snapshot["Options:Value"]
		if value == nil || *value != "43" {
			t.Fatalf("unexpected callback snapshot %#v", snapshot)
		}
	})
	defer unsubscribe()

	updated := store.Update([]ConfigEntry{
		{Key: "Options:Value", Value: ptr("43"), Generation: 2, Timestamp: time.Now()},
	}, true)

	if !updated || !called {
		t.Fatalf("expected update and callback, updated=%v called=%v", updated, called)
	}
}
