namespace Editor.Domain.Graph;

public sealed record NodeParameterDefinition(
    string Name,
    ParameterValueKind Kind,
    ParameterValue DefaultValue,
    float? MinFloat = null,
    float? MaxFloat = null,
    int? MinInt = null,
    int? MaxInt = null,
    IReadOnlyList<string>? EnumValues = null)
{
    public void Validate(ParameterValue value)
    {
        if (value.Kind != Kind)
        {
            throw new InvalidOperationException(
                $"Parameter '{Name}' expects type '{Kind}', received '{value.Kind}'.");
        }

        switch (Kind)
        {
            case ParameterValueKind.Float:
            {
                var typed = value.AsFloat();
                if (MinFloat.HasValue && typed < MinFloat.Value)
                {
                    throw new InvalidOperationException(
                        $"Parameter '{Name}' must be >= {MinFloat.Value}.");
                }

                if (MaxFloat.HasValue && typed > MaxFloat.Value)
                {
                    throw new InvalidOperationException(
                        $"Parameter '{Name}' must be <= {MaxFloat.Value}.");
                }

                break;
            }
            case ParameterValueKind.Integer:
            {
                var typed = value.AsInteger();
                if (MinInt.HasValue && typed < MinInt.Value)
                {
                    throw new InvalidOperationException(
                        $"Parameter '{Name}' must be >= {MinInt.Value}.");
                }

                if (MaxInt.HasValue && typed > MaxInt.Value)
                {
                    throw new InvalidOperationException(
                        $"Parameter '{Name}' must be <= {MaxInt.Value}.");
                }

                break;
            }
            case ParameterValueKind.Enum:
            {
                if (EnumValues is null || EnumValues.Count == 0)
                {
                    throw new InvalidOperationException($"Parameter '{Name}' has no enum values configured.");
                }

                var typed = value.AsEnum();
                if (!EnumValues.Contains(typed, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Parameter '{Name}' does not allow value '{typed}'.");
                }

                break;
            }
            case ParameterValueKind.Boolean:
                value.AsBoolean();
                break;
            default:
                throw new InvalidOperationException($"Unsupported parameter kind '{Kind}'.");
        }
    }
}
