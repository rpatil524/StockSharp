namespace StockSharp.Messages;

/// <summary>
/// Measure units.
/// </summary>
[Serializable]
[DataContract]
public enum UnitTypes
{
	/// <summary>
	/// The absolute value. Incremental change is a given number.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AbsoluteKey)]
	Absolute,

	/// <summary>
	/// Percents. Step change - one hundredth of a percent.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PercentKey)]
	Percent,

	/// <summary>
	/// Point.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PointKey)]
	Point,

	/// <summary>
	/// Price step.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PriceStepKey)]
	Step,

	/// <summary>
	/// The limited value. This unit allows to set a specific change number, which cannot be used in arithmetic operations <see cref="Unit"/>.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LimitKey)]
	Limit,
}

/// <summary>
/// Special class, allows to set the value as a percentage, absolute, points and pips values.
/// </summary>
[Serializable]
[DataContract]
public partial class Unit : Equatable<Unit>, IOperable<Unit>, IPersistable, IFormattable
{
	private class UnitOperator : BaseOperator<Unit>
	{
		public override Unit Add(Unit first, Unit second) => first + second;
		public override int Compare(Unit x, Unit y) => x.CompareTo(y);
		public override Unit Divide(Unit first, Unit second) => first / second;
		public override Unit Multiply(Unit first, Unit second) => first * second;
		public override Unit Subtract(Unit first, Unit second) => first - second;
	}

	static Unit()
	{
		Converter.AddTypedConverter<Unit, decimal>(input => (decimal)input);
		Converter.AddTypedConverter<Unit, double>(input => (double)input);
		Converter.AddTypedConverter<decimal, Unit>(input => input);
		Converter.AddTypedConverter<int, Unit>(input => input);
		Converter.AddTypedConverter<double, Unit>(input => input);

		OperatorRegistry.AddOperator(new UnitOperator());
	}

	/// <summary>
	/// Create unit.
	/// </summary>
	public Unit()
	{
	}

	/// <summary>
	/// Create absolute value <see cref="UnitTypes.Absolute"/>.
	/// </summary>
	/// <param name="value">Value.</param>
	public Unit(decimal value)
		: this(value, UnitTypes.Absolute)
	{
	}

	/// <summary>
	/// Create a value of types <see cref="UnitTypes.Absolute"/> and <see cref="UnitTypes.Percent"/>.
	/// </summary>
	/// <param name="value">Value.</param>
	/// <param name="type">Measure unit.</param>
	public Unit(decimal value, UnitTypes type)
		: this(value, type, null)
	{
	}

	/// <summary>
	/// Create a value of types <see cref="UnitTypes.Point"/> and <see cref="UnitTypes.Step"/>.
	/// </summary>
	/// <param name="value">Value.</param>
	/// <param name="type">Measure unit.</param>
	/// <param name="getTypeValue">The handler returns a value associated with <see cref="Unit.Type"/> (price or volume steps).</param>
	public Unit(decimal value, UnitTypes type, Func<UnitTypes, decimal?> getTypeValue)
	{
		// mika current check should be do while arithmetics operations
		//
		//if (type == UnitTypes.Point || type == UnitTypes.Step)
		//{
		//    if (security is null)
		//        throw new ArgumentException("Type has invalid value '{0}' while security is not set.".Put(type), "type");
		//}

		Value = value;
		Type = type;
		GetTypeValue = getTypeValue;
	}

	/// <summary>
	/// Measure unit.
	/// </summary>
	[DataMember]
	public UnitTypes Type { get; set; }

	/// <summary>
	/// Value.
	/// </summary>
	[DataMember]
	public decimal Value { get; set; }

	[field: NonSerialized]
	private Func<UnitTypes, decimal?> _getTypeValue;

	/// <summary>
	/// The handler returns a value associated with <see cref="Unit.Type"/> (price or volume steps).
	/// </summary>
	[XmlIgnore]
	public Func<UnitTypes, decimal?> GetTypeValue
	{
		get => _getTypeValue;
		set => _getTypeValue = value;
	}

	/// <summary>
	/// Create a copy of <see cref="Unit"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Unit Clone()
	{
		return new()
		{
			Type = Type,
			Value = Value,
			GetTypeValue = GetTypeValue,
		};
	}

	/// <summary>
	/// Compare <see cref="Unit"/> on the equivalence.
	/// </summary>
	/// <param name="other">Another value with which to compare.</param>
	/// <returns>The result of the comparison.</returns>
	public override int CompareTo(Unit other)
	{
		if (this == other)
			return 0;

		if (this < other)
			return -1;

		return 1;
	}

	private decimal SafeGetTypeValue(Func<UnitTypes, decimal?> getTypeValue)
	{
		var func = (GetTypeValue ?? getTypeValue) ?? throw new InvalidOperationException(LocalizedStrings.UnitHandlerNotSet);

		var value = func(Type);

		if (value is not null and not 0)
			return value.Value;

		if (getTypeValue is null)
			throw new ArgumentNullException(nameof(getTypeValue));

		value = getTypeValue(Type);

		if (value is null or 0)
			throw new InvalidOperationException(LocalizedStrings.InvalidValue);

		return value.Value;
	}

	private static Unit CreateResult(Unit u1, Unit u2, Func<decimal, decimal, decimal> operation, Func<decimal, decimal, decimal> percentOperation)
	{
		//  prevent operator '==' call
		if (u1 is null)
			return null;

		if (u2 is null)
			return null;

		if (u1.Type == UnitTypes.Limit || u2.Type == UnitTypes.Limit)
			throw new ArgumentException(LocalizedStrings.LimitedValueNotMath);

		if (operation is null)
			throw new ArgumentNullException(nameof(operation));

		if (percentOperation is null)
			throw new ArgumentNullException(nameof(percentOperation));

		//if (u1.CheckGetTypeValue(false) != u2.CheckGetTypeValue(false))
		//	throw new ArgumentException("One of the values has uninitialized value handler.");

		var result = new Unit
		{
			Type = u1.Type,
			GetTypeValue = u1.GetTypeValue ?? u2.GetTypeValue
		};

		if (u1.Type == u2.Type)
		{
			result.Value = operation(u1.Value, u2.Value);
		}
		else
		{
			if (u1.Type == UnitTypes.Percent || u2.Type == UnitTypes.Percent)
			{
				result.Type = u1.Type == UnitTypes.Percent ? u2.Type : u1.Type;

				var nonPerValue = u1.Type == UnitTypes.Percent ? u2.Value : u1.Value;
				var perValue = u1.Type == UnitTypes.Percent ? u1.Value : u2.Value;

				result.Value = percentOperation(nonPerValue, perValue * nonPerValue.Abs() / 100.0m);
			}
			else
			{
				var value = operation((decimal)u1, (decimal)u2);

				switch (result.Type)
				{
					case UnitTypes.Absolute:
						break;
					case UnitTypes.Point:
						value /= u1.SafeGetTypeValue(result.GetTypeValue);
						break;
					case UnitTypes.Step:
						value /= u1.SafeGetTypeValue(result.GetTypeValue);
						break;
					default:
						throw new ArgumentOutOfRangeException(result.Type.ToString());
				}

				result.Value = value;
			}
		}

		return result;
	}

	/// <summary>
	/// Add the two objects <see cref="Unit"/>.
	/// </summary>
	/// <param name="u1">First object <see cref="Unit"/>.</param>
	/// <param name="u2">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of addition.</returns>
	public static Unit operator +(Unit u1, Unit u2)
	{
		return CreateResult(u1, u2, (v1, v2) => v1 + v2, (nonPer, per) => nonPer + per);
	}

	/// <summary>
	/// Multiply the two objects <see cref="Unit"/>.
	/// </summary>
	/// <param name="u1">First object <see cref="Unit"/>.</param>
	/// <param name="u2">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of the multiplication.</returns>
	public static Unit operator *(Unit u1, Unit u2)
	{
		return CreateResult(u1, u2, (v1, v2) => v1 * v2, (nonPer, per) => nonPer * per);
	}

	/// <summary>
	/// Subtract the unit <see cref="Unit"/> from another.
	/// </summary>
	/// <param name="u1">First object <see cref="Unit"/>.</param>
	/// <param name="u2">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of the subtraction.</returns>
	public static Unit operator -(Unit u1, Unit u2)
	{
		return CreateResult(u1, u2, (v1, v2) => v1 - v2, (nonPer, per) => (u1.Type == UnitTypes.Percent ? (per - nonPer) : (nonPer - per)));
	}

	/// <summary>
	/// Divide the unit <see cref="Unit"/> to another.
	/// </summary>
	/// <param name="u1">First object <see cref="Unit"/>.</param>
	/// <param name="u2">Second object <see cref="Unit"/>.</param>
	/// <returns>The result of the division.</returns>
	public static Unit operator /(Unit u1, Unit u2)
	{
		return CreateResult(u1, u2, (v1, v2) => v1 / v2, (nonPer, per) => u1.Type == UnitTypes.Percent ? per / nonPer : nonPer / per);
	}

	/// <summary>
	/// Get the hash code of the object <see cref="Unit"/>.
	/// </summary>
	/// <returns>A hash code.</returns>
	public override int GetHashCode() => Type.GetHashCode() ^ Value.GetHashCode();

	private bool? EqualsImpl(Unit other)
	{
		//var retVal = Type == other.Type && Value == other.Value;

		//if (Type == UnitTypes.Percent || Type == UnitTypes.Absolute || Type == UnitTypes.Limit)
		//	return retVal;

		//return retVal && CheckGetTypeValue(true) == other.CheckGetTypeValue(true);

		if (Type == other.Type)
			return Value == other.Value;

		if (Type == UnitTypes.Percent || other.Type == UnitTypes.Percent)
			return false;

		if (Type == UnitTypes.Limit || other.Type == UnitTypes.Limit)
			return false;

		//if (GetTypeValue is null || other.GetTypeValue is null)
		//	return false;

		var curr = this;

		if (curr.Type is UnitTypes.Point or UnitTypes.Step && curr.GetTypeValue is null)
			return false;

		if (other.Type is UnitTypes.Point or UnitTypes.Step && other.GetTypeValue is null)
			return false;

		if (other.Type == UnitTypes.Absolute)
		{
			curr = Convert(other.Type, false);

			if (curr is null)
				return null;
		}
		else
		{
			other = other.Convert(Type, false);

			if (other is null)
				return null;
		}

		return curr.Value == other.Value;
	}

	/// <summary>
	/// Compare <see cref="Unit"/> on the equivalence.
	/// </summary>
	/// <param name="other">Another value with which to compare.</param>
	/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
	protected override bool OnEquals(Unit other) => EqualsImpl(other) == true;

	/// <summary>
	/// Compare <see cref="Unit"/> on the equivalence.
	/// </summary>
	/// <param name="other">Another value with which to compare.</param>
	/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
	public override bool Equals(object other) => base.Equals(other);

	/// <summary>
	/// Compare two values in the inequality (if the value of different types, the conversion will be used).
	/// </summary>
	/// <param name="u1">First unit.</param>
	/// <param name="u2">Second unit.</param>
	/// <returns><see langword="true" />, if the values are equals, otherwise, <see langword="false" />.</returns>
	public static bool operator !=(Unit u1, Unit u2)
	{
		if (u1 is null)
			return u2 is not null;

		if (u2 is null)
			return true;

		var res = u1.EqualsImpl(u2);

		if (res == null)
			return false;

		return !res.Value;
	}

	/// <summary>
	/// Compare two values for equality (if the value of different types, the conversion will be used).
	/// </summary>
	/// <param name="u1">First unit.</param>
	/// <param name="u2">Second unit.</param>
	/// <returns><see langword="true" />, if the values are equals, otherwise, <see langword="false" />.</returns>
	public static bool operator ==(Unit u1, Unit u2)
	{
		if (u1 is null)
			return u2 is null;

		if (u2 is null)
			return false;

		return u1.OnEquals(u2);
	}

	/// <inheritdoc />
	public override string ToString()
		=> ToString(null, null);

	/// <inheritdoc/>
	public string ToString(string format, IFormatProvider formatProvider)
		=> Value.ToString(format, formatProvider) + GetTypeSuffix(Type);

	/// <summary>
	/// Get string suffix.
	/// </summary>
	/// <param name="type">Measure unit.</param>
	/// <returns>String suffix.</returns>
	public static string GetTypeSuffix(UnitTypes type)
	{
		return type switch
		{
			UnitTypes.Percent	=> "%",
			UnitTypes.Absolute	=> string.Empty,
			UnitTypes.Step		=> "s",
			UnitTypes.Point		=> "p",
			UnitTypes.Limit		=> "l",

			_ => throw new InvalidOperationException(LocalizedStrings.UnknownUnitMeasurement.Put(type)),
		};
	}

	/// <summary>
	/// Cast the value to another type.
	/// </summary>
	/// <param name="destinationType">Destination value type.</param>
	/// <param name="throwException">Throw exception in case of impossible conversion. Otherwise, returns <see langword="null"/>.</param>
	/// <returns>Converted value.</returns>
	public Unit Convert(UnitTypes destinationType, bool throwException = true)
	{
		return Convert(destinationType, GetTypeValue, throwException);
	}

	private decimal ToDecimal(Func<UnitTypes, decimal?> getTypeValue)
	{
		var value = Value;

		switch (Type)
		{
			case UnitTypes.Limit:
			case UnitTypes.Absolute:
				return value;
			case UnitTypes.Percent:
				throw new InvalidOperationException(LocalizedStrings.PercentagesConvert);
			case UnitTypes.Point:
				var point = getTypeValue?.Invoke(Type) ?? throw new InvalidOperationException(LocalizedStrings.PriceStepNotSpecified);

				if (point == 0)
					throw new InvalidOperationException(LocalizedStrings.PriceStepIsZeroKey);

				return value * point;
			case UnitTypes.Step:
				var step = getTypeValue?.Invoke(Type) ?? throw new InvalidOperationException(LocalizedStrings.PriceStepNotSpecified);

				if (step == 0)
					throw new InvalidOperationException(LocalizedStrings.PriceStepNotSpecified);

				return value * step;
			default:
				throw new InvalidOperationException(Type.ToString());
		}
	}

	/// <summary>
	/// Cast the value to another type.
	/// </summary>
	/// <param name="destinationType">Destination value type.</param>
	/// <param name="getTypeValue">The handler returns a value associated with <see cref="Type"/> (price or volume steps).</param>
	/// <param name="throwException">Throw exception in case of impossible conversion. Otherwise, returns <see langword="null"/>.</param>
	/// <returns>Converted value.</returns>
	public Unit Convert(UnitTypes destinationType, Func<UnitTypes, decimal?> getTypeValue, bool throwException = true)
	{
		if (Type == destinationType)
			return Clone();

		if (Type == UnitTypes.Percent || destinationType == UnitTypes.Percent)
			throw new InvalidOperationException(LocalizedStrings.PercentagesConvert);

		getTypeValue ??= GetTypeValue;

		var value = ToDecimal(getTypeValue);

		if (destinationType is UnitTypes.Point or UnitTypes.Step)
		{
			if (getTypeValue is null)
			{
				if (throwException)
					throw new ArgumentException(LocalizedStrings.UnitHandlerNotSet, nameof(destinationType));

				return null;
			}

			switch (destinationType)
			{
				case UnitTypes.Point:
					var point = getTypeValue(UnitTypes.Point);

					if (point is null or 0)
					{
						if (throwException)
							throw new InvalidOperationException(LocalizedStrings.PriceStepIsZeroKey);

						return null;
					}

					value = value / point.Value;
					break;
				case UnitTypes.Step:
					var step = getTypeValue(UnitTypes.Step);

					if (step is null or 0)
					{
						if (throwException)
							throw new InvalidOperationException(LocalizedStrings.PriceStepNotSpecified);

						return null;
					}

					value = value / step.Value;
					break;
			}
		}

		return new(value, destinationType, getTypeValue);
	}

	private static bool? MoreThan(Unit u1, Unit u2)
	{
		if (u1 is null)
			return null;

		if (u2 is null)
			return null;

		//if (u1.Type == UnitTypes.Limit || u2.Type == UnitTypes.Limit)
		//	throw new ArgumentException("Limit value cannot be modified while arithmetics operations.");

		//if (u1.CheckGetTypeValue(false) != u2.CheckGetTypeValue(false))
		//	throw new ArgumentException("One of the values has uninitialized value handler.");

		if (u1.Type != u2.Type)
		{
			if (u1.Type == UnitTypes.Percent || u2.Type == UnitTypes.Percent)
			{
				return null;
				//throw new ArgumentException(LocalizedStrings.PercentagesCannotCompare.Put(u1, u2));
			}

			if (u2.Type == UnitTypes.Absolute)
			{
				u1 = u1.Convert(u2.Type, false);

				if (u1 is null)
					return null;
			}
			else
			{
				u2 = u2.Convert(u1.Type, false);

				if (u2 is null)
					return null;
			}
		}

		return u1.Value > u2.Value;
	}

	/// <summary>
	/// Check whether the first value is greater than the second.
	/// </summary>
	/// <param name="u1">First unit.</param>
	/// <param name="u2">Second unit.</param>
	/// <returns><see langword="true" />, if the first value is greater than the second, <see langword="false" />.</returns>
	public static bool operator >(Unit u1, Unit u2) => MoreThan(u1, u2) == true;

	/// <summary>
	/// Check whether the first value is greater than or equal to the second.
	/// </summary>
	/// <param name="u1">First unit.</param>
	/// <param name="u2">Second unit.</param>
	/// <returns><see langword="true" />, if the first value is greater than or equal the second, otherwise, <see langword="false" />.</returns>
	public static bool operator >=(Unit u1, Unit u2) => u1 == u2 || u1 > u2;

	/// <summary>
	/// Check whether the first value is less than the second.
	/// </summary>
	/// <param name="u1">First unit.</param>
	/// <param name="u2">Second unit.</param>
	/// <returns><see langword="true" />, if the first value is less than the second, <see langword="false" />.</returns>
	public static bool operator <(Unit u1, Unit u2) => MoreThan(u2, u1) == true;

	/// <summary>
	/// Check whether the first value is less than or equal to the second.
	/// </summary>
	/// <param name="u1">First unit.</param>
	/// <param name="u2">Second unit.</param>
	/// <returns><see langword="true" />, if the first value is less than or equal to the second, <see langword="false" />.</returns>
	public static bool operator <=(Unit u1, Unit u2) => u1 == u2 || MoreThan(u2, u1) == true;

	/// <summary>
	/// Get the value with the opposite sign from the value <see cref="Unit.Value"/>.
	/// </summary>
	/// <param name="u">Unit.</param>
	/// <returns>Opposite value.</returns>
	public static Unit operator -(Unit u)
	{
		if (u is null)
			return null;

		return new Unit
		{
			GetTypeValue = u.GetTypeValue,
			Type = u.Type,
			Value = -u.Value
		};
	}

	Unit IOperable<Unit>.Add(Unit other) => this + other;
	Unit IOperable<Unit>.Subtract(Unit other) => this - other;
	Unit IOperable<Unit>.Multiply(Unit other) => this * other;
	Unit IOperable<Unit>.Divide(Unit other) => this / other;

	/// <summary>
	/// Make the value positive.
	/// </summary>
	/// <returns><see cref="Unit"/></returns>
	public Unit Abs()
		=> new() { Type = Type, Value = Value.Abs(), GetTypeValue = GetTypeValue };

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		Type = storage.GetValue<UnitTypes>(nameof(Type));
		Value = storage.GetValue<decimal>(nameof(Value));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Type), Type.To<string>());
		storage.SetValue(nameof(Value), Value);
	}
}