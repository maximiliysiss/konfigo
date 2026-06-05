package konfigo

import (
	"testing"
	"time"
)

type commonOptions struct {
	Group        `konfigo:"key=CommonOptions,name=Common,description=Description"`
	IntValue     int            `konfigo:"key"`
	StrValue     string         `konfigo:"key"`
	EnumValue    string         `konfigo:"key,type=enum,enum=A|B"`
	Datetime     time.Time      `konfigo:"key"`
	Duration     time.Duration  `konfigo:"key"`
	ArrayValue   []int          `konfigo:"key"`
	JSONValue    map[string]int `konfigo:"key"`
	BoolValue    bool           `konfigo:"key"`
	OptionalText *string        `konfigo:"key"`
}

func TestDiscoverDefinitionsMapsSupportedTypesAndDefaults(t *testing.T) {
	definitions, err := DiscoverDefinitions(commonOptions{})
	if err != nil {
		t.Fatal(err)
	}
	if len(definitions) != 1 {
		t.Fatalf("expected one definition, got %d", len(definitions))
	}

	definition := definitions[0]
	if definition.Key != "CommonOptions" {
		t.Fatalf("unexpected definition key %q", definition.Key)
	}
	if definition.Name != "Common" {
		t.Fatalf("unexpected definition name %q", definition.Name)
	}
	if definition.Description == nil || *definition.Description != "Description" {
		t.Fatalf("unexpected description %#v", definition.Description)
	}

	options := map[string]OptionDefinition{}
	for _, option := range definition.Options {
		options[option.Name] = option
	}

	assertOption(t, options["IntValue"], ValueTypeNumber, ptr("0"))
	assertOption(t, options["StrValue"], ValueTypeString, ptr(""))
	assertOption(t, options["EnumValue"], ValueTypeEnum, nil)
	assertOption(t, options["Datetime"], ValueTypeDateTime, ptr("0001-01-01T00:00:00Z"))
	assertOption(t, options["Duration"], ValueTypeTimeSpan, ptr("00:00:00"))
	assertOption(t, options["ArrayValue"], ValueTypeArray, ptr("[]"))
	assertOption(t, options["JSONValue"], ValueTypeJSON, ptr("{}"))
	assertOption(t, options["BoolValue"], ValueTypeBoolean, ptr("false"))
	assertOption(t, options["OptionalText"], ValueTypeString, nil)

	if got := options["EnumValue"].EnumValues; len(got) != 2 || got[0] != "A" || got[1] != "B" {
		t.Fatalf("unexpected enum values %#v", got)
	}
}

type customOptions struct {
	Group `konfigo:"key=custom-key,name=Custom,description=Group description"`
	Value int `konfigo:"key,name=Value,description=Option description,default=7"`
}

func TestCustomGroupAndKeyMetadata(t *testing.T) {
	definitions, err := DiscoverDefinitions(customOptions{})
	if err != nil {
		t.Fatal(err)
	}

	definition := definitions[0]
	option := definition.Options[0]

	if definition.Key != "custom-key" {
		t.Fatalf("unexpected group key %q", definition.Key)
	}
	if option.Key != "custom-key:Value" {
		t.Fatalf("unexpected option key %q", option.Key)
	}
	if option.Name != "Value" {
		t.Fatalf("unexpected option name %q", option.Name)
	}
	if option.Description == nil || *option.Description != "Option description" {
		t.Fatalf("unexpected option description %#v", option.Description)
	}
	if option.DefaultValue == nil || *option.DefaultValue != "7" {
		t.Fatalf("unexpected default %#v", option.DefaultValue)
	}
}

func assertOption(t *testing.T, option OptionDefinition, valueType ValueType, defaultValue *string) {
	t.Helper()

	if option.Type != valueType {
		t.Fatalf("%s: expected type %d, got %d", option.Name, valueType, option.Type)
	}
	if (option.DefaultValue == nil) != (defaultValue == nil) {
		t.Fatalf("%s: expected default %#v, got %#v", option.Name, defaultValue, option.DefaultValue)
	}
	if defaultValue != nil && *option.DefaultValue != *defaultValue {
		t.Fatalf("%s: expected default %q, got %q", option.Name, *defaultValue, *option.DefaultValue)
	}
}

func ptr(value string) *string {
	return &value
}
