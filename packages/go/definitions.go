package konfigo

import (
	"fmt"
	"reflect"
	"strconv"
	"strings"
	"time"
)

type Group struct{}

type ClassDefinition struct {
	Key         string
	Name        string
	Description *string
	Options     []OptionDefinition
	Type        reflect.Type
}

type OptionDefinition struct {
	Key          string
	Name         string
	Description  *string
	Type         ValueType
	DefaultValue *string
	EnumValues   []string
}

func DiscoverDefinitions(values ...any) ([]ClassDefinition, error) {
	var definitions []ClassDefinition

	for _, value := range values {
		definition, ok, err := DefinitionFromStruct(value)
		if err != nil {
			return nil, err
		}
		if ok && len(definition.Options) > 0 {
			definitions = append(definitions, definition)
		}
	}

	return definitions, nil
}

func DefinitionFromStruct(value any) (ClassDefinition, bool, error) {
	t := reflect.TypeOf(value)
	if t == nil {
		return ClassDefinition{}, false, fmt.Errorf("konfigo: nil value is not a config group")
	}
	if t.Kind() == reflect.Pointer {
		t = t.Elem()
	}
	if t.Kind() != reflect.Struct {
		return ClassDefinition{}, false, fmt.Errorf("konfigo: %s is not a struct", t)
	}

	groupField, ok := findGroupField(t)
	if !ok {
		return ClassDefinition{}, false, nil
	}

	groupTag := parseTag(groupField.Tag.Get("konfigo"))
	sectionKey := firstNonEmpty(groupTag["key"], t.Name())
	definition := ClassDefinition{
		Key:         sectionKey,
		Name:        firstNonEmpty(groupTag["name"], groupTag["group_name"], t.Name()),
		Description: optionalString(groupTag["description"]),
		Type:        t,
	}

	for i := 0; i < t.NumField(); i++ {
		field := t.Field(i)
		if field.Anonymous && field.Type == reflect.TypeOf(Group{}) {
			continue
		}
		if !field.IsExported() {
			continue
		}

		tag := parseTag(field.Tag.Get("konfigo"))
		if tag["key"] != "true" && !hasAny(tag, "name", "description", "default", "type", "enum") {
			continue
		}

		option, err := optionFromField(sectionKey, field, tag)
		if err != nil {
			return ClassDefinition{}, false, err
		}
		definition.Options = append(definition.Options, option)
	}

	return definition, true, nil
}

func optionFromField(sectionKey string, field reflect.StructField, tag map[string]string) (OptionDefinition, error) {
	valueType, err := mapValueType(field.Type, tag)
	if err != nil {
		return OptionDefinition{}, fmt.Errorf("konfigo: %s.%s: %w", field.Type.Name(), field.Name, err)
	}

	defaultValue, hasDefault := tag["default"]
	if !hasDefault && !isOptional(field.Type) {
		defaultValue, hasDefault = mapDefaultValue(valueType)
	}

	enumValues := splitValues(firstNonEmpty(tag["enum"], tag["enum_values"]))

	return OptionDefinition{
		Key:          sectionKey + ":" + field.Name,
		Name:         firstNonEmpty(tag["name"], field.Name),
		Description:  optionalString(tag["description"]),
		Type:         valueType,
		DefaultValue: optionalStringIf(hasDefault, defaultValue),
		EnumValues:   enumValues,
	}, nil
}

func findGroupField(t reflect.Type) (reflect.StructField, bool) {
	for i := 0; i < t.NumField(); i++ {
		field := t.Field(i)
		tag := parseTag(field.Tag.Get("konfigo"))
		if field.Anonymous && field.Type == reflect.TypeOf(Group{}) {
			return field, true
		}
		if tag["group"] == "true" {
			return field, true
		}
	}
	return reflect.StructField{}, false
}

func mapValueType(t reflect.Type, tag map[string]string) (ValueType, error) {
	if override := tag["type"]; override != "" {
		return parseValueType(override)
	}
	if tag["enum"] != "" || tag["enum_values"] != "" {
		return ValueTypeEnum, nil
	}

	t = derefType(t)
	if t == reflect.TypeOf(time.Time{}) {
		return ValueTypeDateTime, nil
	}
	if t == reflect.TypeOf(time.Duration(0)) {
		return ValueTypeTimeSpan, nil
	}

	switch t.Kind() {
	case reflect.String:
		return ValueTypeString, nil
	case reflect.Bool:
		return ValueTypeBoolean, nil
	case reflect.Int, reflect.Int8, reflect.Int16, reflect.Int32, reflect.Int64,
		reflect.Uint, reflect.Uint8, reflect.Uint16, reflect.Uint32, reflect.Uint64,
		reflect.Float32, reflect.Float64:
		return ValueTypeNumber, nil
	case reflect.Slice, reflect.Array:
		return ValueTypeArray, nil
	case reflect.Map, reflect.Struct:
		return ValueTypeJSON, nil
	default:
		return ValueTypeJSON, nil
	}
}

func parseValueType(value string) (ValueType, error) {
	switch strings.ToLower(value) {
	case "string":
		return ValueTypeString, nil
	case "boolean", "bool":
		return ValueTypeBoolean, nil
	case "date_time", "datetime", "date-time":
		return ValueTypeDateTime, nil
	case "time_span", "timespan", "duration":
		return ValueTypeTimeSpan, nil
	case "enum":
		return ValueTypeEnum, nil
	case "number":
		return ValueTypeNumber, nil
	case "array":
		return ValueTypeArray, nil
	case "json":
		return ValueTypeJSON, nil
	default:
		return ValueTypeUnknown, fmt.Errorf("unknown value type %q", value)
	}
}

func mapDefaultValue(valueType ValueType) (string, bool) {
	switch valueType {
	case ValueTypeArray:
		return "[]", true
	case ValueTypeString:
		return "", true
	case ValueTypeDateTime:
		return time.Time{}.UTC().Format(time.RFC3339), true
	case ValueTypeTimeSpan:
		return "00:00:00", true
	case ValueTypeJSON:
		return "{}", true
	case ValueTypeNumber:
		return "0", true
	case ValueTypeBoolean:
		return "false", true
	default:
		return "", false
	}
}

func parseTag(raw string) map[string]string {
	result := map[string]string{}
	for _, part := range strings.Split(raw, ",") {
		part = strings.TrimSpace(part)
		if part == "" {
			continue
		}
		key, value, ok := strings.Cut(part, "=")
		key = strings.TrimSpace(key)
		if !ok {
			result[key] = "true"
			continue
		}
		if unquoted, err := strconv.Unquote(strings.TrimSpace(value)); err == nil {
			value = unquoted
		}
		result[key] = strings.TrimSpace(value)
	}
	return result
}

func splitValues(raw string) []string {
	if raw == "" {
		return nil
	}
	separator := "|"
	if strings.Contains(raw, ";") {
		separator = ";"
	}
	values := strings.Split(raw, separator)
	result := make([]string, 0, len(values))
	for _, value := range values {
		value = strings.TrimSpace(value)
		if value != "" {
			result = append(result, value)
		}
	}
	return result
}

func derefType(t reflect.Type) reflect.Type {
	for t.Kind() == reflect.Pointer {
		t = t.Elem()
	}
	return t
}

func isOptional(t reflect.Type) bool {
	return t.Kind() == reflect.Pointer
}

func optionalString(value string) *string {
	if value == "" {
		return nil
	}
	return &value
}

func optionalStringIf(ok bool, value string) *string {
	if !ok {
		return nil
	}
	return &value
}

func firstNonEmpty(values ...string) string {
	for _, value := range values {
		if value != "" {
			return value
		}
	}
	return ""
}

func hasAny(values map[string]string, keys ...string) bool {
	for _, key := range keys {
		if _, ok := values[key]; ok {
			return true
		}
	}
	return false
}
