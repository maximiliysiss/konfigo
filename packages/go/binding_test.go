package konfigo

import (
	"testing"
	"time"
)

type bindOptions struct {
	Group   `konfigo:"key=Bind"`
	Count   int           `konfigo:"key"`
	Enabled bool          `konfigo:"key"`
	Delay   time.Duration `konfigo:"key"`
	Tags    []string      `konfigo:"key"`
}

func TestBindConfigParsesValues(t *testing.T) {
	options := bindOptions{}

	err := BindConfig(map[string]*string{
		"Bind:Count":   ptr("7"),
		"Bind:Enabled": ptr("true"),
		"Bind:Delay":   ptr("00:00:05"),
		"Bind:Tags":    ptr(`["a","b"]`),
	}, &options)
	if err != nil {
		t.Fatal(err)
	}

	if options.Count != 7 {
		t.Fatalf("unexpected count %d", options.Count)
	}
	if !options.Enabled {
		t.Fatal("expected enabled to be true")
	}
	if options.Delay != 5*time.Second {
		t.Fatalf("unexpected delay %s", options.Delay)
	}
	if len(options.Tags) != 2 || options.Tags[0] != "a" || options.Tags[1] != "b" {
		t.Fatalf("unexpected tags %#v", options.Tags)
	}
}
