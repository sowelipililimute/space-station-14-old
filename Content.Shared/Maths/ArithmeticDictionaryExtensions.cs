using System.Numerics;
using Robust.Shared.Utility;

namespace Content.Shared.Maths;

public static class ArithmeticDictionaryExtensions
{
    extension<TKey, TValue>(Dictionary<TKey, TValue> target)
        where TKey : notnull
        where TValue : IComparisonOperators<TValue, TValue, bool>,
            IAdditiveIdentity<TValue, TValue>,
            IAdditionOperators<TValue, TValue, TValue>,
            ISubtractionOperators<TValue, TValue, TValue>,
            IMultiplyOperators<TValue, TValue, TValue>,
            IDivisionOperators<TValue, TValue, TValue>,
            IUnaryNegationOperators<TValue, TValue>
    {
        public TValue GetTotal()
        {
            var total = TValue.AdditiveIdentity;

            foreach (var value in target.Values)
            {
                total += value;
            }

            return total;
        }

        public bool AnyPositive()
        {
            foreach (var value in target.Values)
            {
                if (value > TValue.AdditiveIdentity)
                    return true;
            }

            return false;
        }

        public Dictionary<TKey, TValue> GetPositive()
        {
            var dict = new Dictionary<TKey, TValue>();

            foreach (var (key, value) in target)
            {
                if (value > TValue.AdditiveIdentity)
                    dict[key] = value;
            }

            return dict;
        }

        public Dictionary<TKey, TValue> GetNegative()
        {
            var dict = new Dictionary<TKey, TValue>();

            foreach (var (key, value) in target)
            {
                if (value < TValue.AdditiveIdentity)
                    dict[key] = value;
            }

            return dict;
        }

        public void TrimZeros()
        {
            foreach (var (key, value) in target)
            {
                if (value == TValue.AdditiveIdentity)
                    target.Remove(key);
            }
        }

        public void Clamp(TValue min, TValue max)
        {
            DebugTools.Assert(min < max);
            foreach (var (key, value) in target)
            {
                target[key] = value < min ? min : value > max ? max : value;
            }
        }

        public void ClampMin(TValue min)
        {
            foreach (var (key, value) in target)
            {
                target[key] = value < min ? min : value;
            }
        }

        public void ClampMax(TValue max)
        {
            foreach (var (key, value) in target)
            {
                target[key] = value < max ? value : max;
            }
        }

        public static Dictionary<TKey, TValue> operator *(Dictionary<TKey, TValue> value, TValue scalar)
        {
            var result = new Dictionary<TKey, TValue>(value);
            foreach (var key in result.Keys)
            {
                result[key] *= scalar;
            }
            return result;
        }

        public static Dictionary<TKey, TValue> operator /(Dictionary<TKey, TValue> value, TValue scalar)
        {
            var result = new Dictionary<TKey, TValue>(value);
            foreach (var key in result.Keys)
            {
                result[key] /= scalar;
            }
            return result;
        }

        public static Dictionary<TKey, TValue> operator +(Dictionary<TKey, TValue> left, Dictionary<TKey, TValue> right)
        {
            var result = new Dictionary<TKey, TValue>(left);
            foreach (var entry in right)
            {
                if (!result.TryAdd(entry.Key, entry.Value))
                    result[entry.Key] += entry.Value;
            }

            return result;
        }

        public static Dictionary<TKey, TValue> operator -(Dictionary<TKey, TValue> left, Dictionary<TKey, TValue> right)
        {
            var result = new Dictionary<TKey, TValue>(left);
            foreach (var entry in right)
            {
                if (!result.TryAdd(entry.Key, -entry.Value))
                    result[entry.Key] -= entry.Value;
            }

            return result;
        }
    }
}
