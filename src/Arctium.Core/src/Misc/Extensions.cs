// Copyright (c) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace Arctium.Core
{
    public static class Extensions
    {
        public static uint LeftRotate(this uint value, int shiftCount) => (value << shiftCount) | (value >> (0x20 - shiftCount));

        public static T ChangeType<T>(this object value) => (T)ChangeType(value, typeof(T));

        public static object ChangeType(this object value, Type destType)
        {
            return destType.IsEnum ? Enum.ToObject(destType, value) : Convert.ChangeType(value, destType);
        }

        public static T MinValue<T>(this Type primitiveType) where T : struct, IComparable => (T)primitiveType.GetField("MinValue").GetRawConstantValue();

        public static T MaxValue<T>(this Type primitiveType) where T : struct, IComparable => (T)primitiveType.GetField("MaxValue").GetRawConstantValue();

        public static bool IsSigned(this Type t) => Convert.ToBoolean(t.GetField("MinValue").GetRawConstantValue());

        public static void AssignValue(this PropertyInfo field, object obj, object value)
        {
            object fieldValue;

            // Primitive types & numeric/string enum options.
            if (field.PropertyType.IsPrimitive || field.PropertyType.IsEnum)
            {
                // Convert the config value to a string.
                var stringValue = value.ToString();

                // Check for hex numbers (starting with 0x).
                var numberBase = stringValue.StartsWith("0x") ? 16 : 10;

                // Parse bool option by string.
                if (field.PropertyType == typeof(bool))
                    fieldValue = stringValue != "0";
                // Parse enum options by string.
                else if (field.PropertyType.IsEnum && numberBase == 10)
                    fieldValue = Enum.Parse(field.PropertyType, stringValue);
                else
                {
                    // Get the true type.
                    var valueType = field.PropertyType.IsEnum ? field.PropertyType.GetEnumUnderlyingType() : field.PropertyType;

                    // Check if it's a signed or unsigned type and convert it to the correct type.
                    if (valueType.IsSigned())
                        fieldValue = Convert.ToInt64(stringValue, numberBase).ChangeType(valueType);
                    else
                        fieldValue = Convert.ToUInt64(stringValue, numberBase).ChangeType(valueType);
                }
            }
            else if (field.PropertyType != typeof(string) && field.PropertyType.IsClass)
            {
                fieldValue = Activator.CreateInstance(field.PropertyType);

                var fieldValues = (Dictionary<string, string>)value;

                // Get class fields.
                foreach (var f in field.PropertyType.GetProperties())
                {
                    if (fieldValues.TryGetValue(f.Name, out var objFieldValue))
                        AssignValue(f, fieldValue, objFieldValue);
                }
            }
            else
            {
                // String values.
                fieldValue = value;
            }

            field.SetValue(obj, fieldValue);
        }

        // TODO: Add long, ulong, float, double support.
        public static T[] FillRandom<T>(this T[] array) where T : struct, IComparable
        {
            var random = new Random((int)((uint)(Guid.NewGuid().GetHashCode() ^ 1 >> 89 << 2 ^ 42)).LeftRotate(13));
            var minValue = typeof(T).MinValue<long>();
            var maxValue = typeof(T).MaxValue<long>();

            if (maxValue > int.MaxValue)
                maxValue = int.MaxValue;

            for (int i = 0; i < array.Length; i++)
            {
                int randomInt;

                do
                {
                    randomInt = (int)((uint)random.Next((int)minValue, (int)maxValue)).LeftRotate(1) ^ i;
                } while (randomInt > maxValue || randomInt <= minValue);

                array[i] = randomInt.ChangeType<T>();
            }

            return array;
        }

        public static byte[] ToByteArray(this string s)
        {
            var data = new byte[s.Length / 2];

            for (int i = 0; i < s.Length; i += 2)
                data[i / 2] = Convert.ToByte(s.Substring(i, 2), 16);

            return data;
        }

        public static string SubString(string s, char seperator) => s.Substring(s.IndexOf(seperator));

        public static bool Compare(this byte[] b, byte[] b2)
        {
            for (int i = 0; i < b2.Length; i++)
                if (b[i] != b2[i])
                    return false;

            return true;
        }

        public static bool Compare(this int[] b, int[] b2)
        {
            for (int i = 0; i < b2.Length; i++)
                if (b[i] != b2[i])
                    return false;

            return true;
        }

        public static byte[] Combine(this byte[] data, params byte[][] pData)
        {
            var combined = data;

            foreach (var arr in pData)
            {
                var currentSize = combined.Length;

                Array.Resize(ref combined, currentSize + arr.Length);

                Buffer.BlockCopy(arr, 0, combined, currentSize, arr.Length);
            }

            return combined;
        }

        public static string ToHexString(this byte[] data)
        {
            var hex = "";

            foreach (var b in data)
                hex += $"{b:X2}";

            return hex;
        }

        public static BigInteger ToBigInteger(this byte[] value, bool reverse = false)
        {
            var data = value as byte[];

            if (reverse)
                Array.Reverse(data);

            return new BigInteger(data.Combine(new byte[] { 0 }));
        }
    }
}
