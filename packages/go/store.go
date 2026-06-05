package konfigo

import (
	"sync"
	"time"
)

const TimestampKey = "RealtimeConfigOptions:Timestamp"

type Store struct {
	mu          sync.RWMutex
	data        map[string]*string
	generations map[string]int32
	timestamp   time.Time
	callbacks   map[int]func(map[string]*string)
	nextID      int
}

func NewStore(entries ...ConfigEntry) *Store {
	store := &Store{
		data:        map[string]*string{},
		generations: map[string]int32{},
		timestamp:   time.Time{}.UTC(),
		callbacks:   map[int]func(map[string]*string){},
	}
	store.Update(entries, false)
	return store
}

func (s *Store) Timestamp() time.Time {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return s.timestamp
}

func (s *Store) Snapshot() map[string]*string {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return copyData(s.data)
}

func (s *Store) Get(key string) (*string, bool) {
	s.mu.RLock()
	defer s.mu.RUnlock()
	value, ok := s.data[key]
	return cloneString(value), ok
}

func (s *Store) Subscribe(callback func(map[string]*string)) func() {
	s.mu.Lock()
	id := s.nextID
	s.nextID++
	s.callbacks[id] = callback
	s.mu.Unlock()

	return func() {
		s.mu.Lock()
		delete(s.callbacks, id)
		s.mu.Unlock()
	}
}

func (s *Store) Update(entries []ConfigEntry, notify bool) bool {
	s.mu.Lock()

	updated := false
	for _, entry := range entries {
		if current, ok := s.generations[entry.Key]; ok && current >= entry.Generation {
			continue
		}

		s.data[entry.Key] = cloneString(entry.Value)
		s.generations[entry.Key] = entry.Generation
		if ts := normalizeTime(entry.Timestamp); ts.After(s.timestamp) {
			s.timestamp = ts
		}
		updated = true
	}

	timestamp := s.timestamp.Format(time.RFC3339Nano)
	s.data[TimestampKey] = &timestamp

	var callbacks []func(map[string]*string)
	var snapshot map[string]*string
	if updated && notify {
		callbacks = make([]func(map[string]*string), 0, len(s.callbacks))
		for _, callback := range s.callbacks {
			callbacks = append(callbacks, callback)
		}
		snapshot = copyData(s.data)
	}

	s.mu.Unlock()

	for _, callback := range callbacks {
		callback(copyData(snapshot))
	}

	return updated
}

func copyData(data map[string]*string) map[string]*string {
	result := make(map[string]*string, len(data))
	for key, value := range data {
		result[key] = cloneString(value)
	}
	return result
}

func cloneString(value *string) *string {
	if value == nil {
		return nil
	}
	cloned := *value
	return &cloned
}

func normalizeTime(value time.Time) time.Time {
	if value.IsZero() {
		return time.Time{}.UTC()
	}
	return value.UTC()
}
