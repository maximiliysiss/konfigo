package konfigo

import (
	"encoding/json"
	"fmt"
	"reflect"
	"strconv"
	"strings"
	"time"
)

func BindConfig(values map[string]*string, target any) error {
	value := reflect.ValueOf(target)
	if value.Kind() != reflect.Pointer || value.IsNil() {
		return fmt.Errorf("konfigo: target must be a non-nil pointer")
	}

	structValue := value.Elem()
	if structValue.Kind() != reflect.Struct {
		return fmt.Errorf("konfigo: target must point to a struct")
	}

	definition, ok, err := DefinitionFromStruct(target)
	if err != nil {
		return err
	}
	if !ok {
		return fmt.Errorf("konfigo: %s is not marked as a config group", structValue.Type().Name())
	}

	for _, option := range definition.Options {
		fieldName := strings.TrimPrefix(option.Key, definition.Key+":")
		field := structValue.FieldByName(fieldName)
		if !field.IsValid() || !field.CanSet() {
			continue
		}

		raw := values[option.Key]
		if raw == nil {
			continue
		}
		if err := setValue(field, *raw); err != nil {
			return fmt.Errorf("konfigo: bind %s: %w", option.Key, err)
		}
	}

	return nil
}

func setValue(field reflect.Value, raw string) error {
	if field.Kind() == reflect.Pointer {
		value := reflect.New(field.Type().Elem())
		if err := setValue(value.Elem(), raw); err != nil {
			return err
		}
		field.Set(value)
		return nil
	}

	if field.Type() == reflect.TypeOf(time.Time{}) {
		parsed, err := time.Parse(time.RFC3339Nano, strings.Replace(raw, "Z", "+00:00", 1))
		if err != nil {
			return err
		}
		field.Set(reflect.ValueOf(parsed))
		return nil
	}
	if field.Type() == reflect.TypeOf(time.Duration(0)) {
		parsed, err := parseDuration(raw)
		if err != nil {
			return err
		}
		field.Set(reflect.ValueOf(parsed))
		return nil
	}

	switch field.Kind() {
	case reflect.String:
		field.SetString(raw)
	case reflect.Bool:
		parsed, err := strconv.ParseBool(strings.ToLower(strings.TrimSpace(raw)))
		if err != nil {
			return err
		}
		field.SetBool(parsed)
	case reflect.Int, reflect.Int8, reflect.Int16, reflect.Int32, reflect.Int64:
		parsed, err := strconv.ParseInt(raw, 10, field.Type().Bits())
		if err != nil {
			return err
		}
		field.SetInt(parsed)
	case reflect.Uint, reflect.Uint8, reflect.Uint16, reflect.Uint32, reflect.Uint64:
		parsed, err := strconv.ParseUint(raw, 10, field.Type().Bits())
		if err != nil {
			return err
		}
		field.SetUint(parsed)
	case reflect.Float32, reflect.Float64:
		parsed, err := strconv.ParseFloat(raw, field.Type().Bits())
		if err != nil {
			return err
		}
		field.SetFloat(parsed)
	case reflect.Slice, reflect.Array, reflect.Map, reflect.Struct:
		return json.Unmarshal([]byte(raw), field.Addr().Interface())
	default:
		return fmt.Errorf("unsupported field type %s", field.Type())
	}

	return nil
}

func parseDuration(raw string) (time.Duration, error) {
	if duration, err := time.ParseDuration(raw); err == nil {
		return duration, nil
	}

	parts := strings.Split(raw, ":")
	if len(parts) != 3 {
		return 0, fmt.Errorf("expected HH:MM:SS duration, got %q", raw)
	}

	hours, err := strconv.Atoi(parts[0])
	if err != nil {
		return 0, err
	}
	minutes, err := strconv.Atoi(parts[1])
	if err != nil {
		return 0, err
	}
	seconds, err := strconv.ParseFloat(parts[2], 64)
	if err != nil {
		return 0, err
	}

	return time.Duration(hours)*time.Hour +
		time.Duration(minutes)*time.Minute +
		time.Duration(seconds*float64(time.Second)), nil
}
