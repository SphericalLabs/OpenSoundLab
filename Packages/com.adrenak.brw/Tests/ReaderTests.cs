using System;
using UnityEngine;
using NUnit.Framework;

namespace Adrenak.BRW.Tests {
    public class ReaderTests {
        BytesReader reader;

        [Test]
        public void ReadByte_ReadsCorrectByte() {
            byte[] data = { 42 };
            reader = new BytesReader(data);
            bool success = reader.ReadByte(out byte result);
            Assert.IsTrue(success);
            Assert.AreEqual(42, result);
            Assert.AreEqual(1, reader.Cursor);
        }

        [Test]
        public void ReadByte_ReturnsFalseOnOutOfBounds() {
            byte[] data = { };
            reader = new BytesReader(data);
            bool success = reader.ReadByte(out byte result);
            Assert.IsFalse(success);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void ReadBytes_ReadsCorrectBytes() {
            byte[] data = { 1, 2, 3, 4, 5 };
            reader = new BytesReader(data);
            byte[] result = reader.ReadBytes(3);
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(1, result[0]);
            Assert.AreEqual(2, result[1]);
            Assert.AreEqual(3, result[2]);
            Assert.AreEqual(3, reader.Cursor);
        }

        [Test]
        public void ReadBytes_ReturnsNullOnOutOfBounds() {
            byte[] data = { 1, 2 };
            reader = new BytesReader(data);
            byte[] result = reader.ReadBytes(5);
            Assert.IsNull(result);
        }

        [Test]
        public void ReadBytesDiscrete_ReadsFromSpecificIndex() {
            byte[] data = { 1, 2, 3, 4, 5 };
            reader = new BytesReader(data);
            byte[] result = reader.ReadBytesDiscrete(2, 2);
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(3, result[0]);
            Assert.AreEqual(4, result[1]);
            Assert.AreEqual(0, reader.Cursor); // Cursor should not change
        }

        [Test]
        public void ReadByteArray_ReadsLengthAndBytes() {
            BytesWriter writer = new BytesWriter();
            byte[] original = { 10, 20, 30 };
            writer.WriteByteArray(original);
            reader = new BytesReader(writer.Bytes);
            byte[] result = reader.ReadByteArray();
            Assert.IsNotNull(result);
            Assert.AreEqual(original, result);
        }

        [Test]
        public void ReadShort_ReadsCorrectValue() {
            BytesWriter writer = new BytesWriter();
            Int16 value = 12345;
            writer.WriteShort(value);
            reader = new BytesReader(writer.Bytes);
            Assert.AreEqual(value, reader.ReadShort());
        }

        [Test]
        public void ReadShortArray_ReadsLengthAndValues() {
            BytesWriter writer = new BytesWriter();
            Int16[] array = { 1, 2, 3, 4, 5 };
            writer.WriteShortArray(array);
            reader = new BytesReader(writer.Bytes);
            var result = reader.ReadShortArray();
            Assert.AreEqual(array, result);
        }

        [Test]
        public void ReadInt_ReadsCorrectValue() {
            BytesWriter writer = new BytesWriter();
            Int32 value = 123456789;
            writer.WriteInt(value);
            reader = new BytesReader(writer.Bytes);
            Assert.AreEqual(value, reader.ReadInt());
        }

        [Test]
        public void ReadIntArray_ReadsLengthAndValues() {
            BytesWriter writer = new BytesWriter();
            Int32[] array = { 100, 200, 300 };
            writer.WriteIntArray(array);
            reader = new BytesReader(writer.Bytes);
            var result = reader.ReadIntArray();
            Assert.AreEqual(array, result);
        }

        [Test]
        public void ReadLong_ReadsCorrectValue() {
            BytesWriter writer = new BytesWriter();
            Int64 value = 123456789012345;
            writer.WriteLong(value);
            reader = new BytesReader(writer.Bytes);
            Assert.AreEqual(value, reader.ReadLong());
        }

        [Test]
        public void ReadLongArray_ReadsLengthAndValues() {
            BytesWriter writer = new BytesWriter();
            Int64[] array = { 1000L, 2000L, 3000L };
            writer.WriteLongArray(array);
            reader = new BytesReader(writer.Bytes);
            var result = reader.ReadLongArray();
            Assert.AreEqual(array, result);
        }

        [Test]
        public void ReadFloat_ReadsCorrectValue() {
            BytesWriter writer = new BytesWriter();
            float value = 3.14159f;
            writer.WriteFloat(value);
            reader = new BytesReader(writer.Bytes);
            Assert.AreEqual(value, reader.ReadFloat(), 0.0001f);
        }

        [Test]
        public void ReadFloatArray_ReadsLengthAndValues() {
            BytesWriter writer = new BytesWriter();
            float[] array = { 1.1f, 2.2f, 3.3f };
            writer.WriteFloatArray(array);
            reader = new BytesReader(writer.Bytes);
            var result = reader.ReadFloatArray();
            Assert.AreEqual(array.Length, result.Length);
            for (int i = 0; i < array.Length; i++) {
                Assert.AreEqual(array[i], result[i], 0.0001f);
            }
        }

        [Test]
        public void ReadDouble_ReadsCorrectValue() {
            BytesWriter writer = new BytesWriter();
            double value = 3.141592653589793;
            writer.WriteDouble(value);
            reader = new BytesReader(writer.Bytes);
            Assert.AreEqual(value, reader.ReadDouble(), 0.0000001);
        }

        [Test]
        public void ReadDoubleArray_ReadsLengthAndValues() {
            BytesWriter writer = new BytesWriter();
            double[] array = { 1.1, 2.2, 3.3 };
            writer.WriteDoubleArray(array);
            reader = new BytesReader(writer.Bytes);
            var result = reader.ReadDoubleArray();
            Assert.AreEqual(array.Length, result.Length);
            for (int i = 0; i < array.Length; i++) {
                Assert.AreEqual(array[i], result[i], 0.0000001);
            }
        }

        [Test]
        public void ReadChar_ReadsCorrectValue() {
            BytesWriter writer = new BytesWriter();
            char value = 'A';
            writer.WriteChar(value);
            reader = new BytesReader(writer.Bytes);
            Assert.AreEqual(value, reader.ReadChar());
        }

        [Test]
        public void ReadString_ReadsCorrectValue() {
            BytesWriter writer = new BytesWriter();
            string value = "Hello, World!";
            writer.WriteString(value);
            reader = new BytesReader(writer.Bytes);
            Assert.AreEqual(value, reader.ReadString());
        }

        [Test]
        public void ReadStringArray_ReadsLengthAndValues() {
            BytesWriter writer = new BytesWriter();
            string[] array = { "Hello", "World", "Test" };
            writer.WriteStringArray(array);
            reader = new BytesReader(writer.Bytes);
            var result = reader.ReadStringArray();
            Assert.AreEqual(array, result);
        }

        [Test]
        public void ReadVector2_ReadsCorrectValue() {
            BytesWriter writer = new BytesWriter();
            Vector2 value = new Vector2(1.5f, 2.5f);
            writer.WriteVector2(value);
            reader = new BytesReader(writer.Bytes);
            Vector2 result = reader.ReadVector2();
            Assert.AreEqual(value.x, result.x, 0.0001f);
            Assert.AreEqual(value.y, result.y, 0.0001f);
        }

        [Test]
        public void ReadVector2Array_ReadsLengthAndValues() {
            BytesWriter writer = new BytesWriter();
            Vector2[] array = { new Vector2(1, 2), new Vector2(3, 4) };
            writer.WriteVector2Array(array);
            reader = new BytesReader(writer.Bytes);
            var result = reader.ReadVector2Array();
            Assert.AreEqual(array.Length, result.Length);
            for (int i = 0; i < array.Length; i++) {
                Assert.AreEqual(array[i].x, result[i].x, 0.0001f);
                Assert.AreEqual(array[i].y, result[i].y, 0.0001f);
            }
        }

        [Test]
        public void ReadVector3_ReadsCorrectValue() {
            BytesWriter writer = new BytesWriter();
            Vector3 value = new Vector3(1.5f, 2.5f, 3.5f);
            writer.WriteVector3(value);
            reader = new BytesReader(writer.Bytes);
            Vector3 result = reader.ReadVector3();
            Assert.AreEqual(value.x, result.x, 0.0001f);
            Assert.AreEqual(value.y, result.y, 0.0001f);
            Assert.AreEqual(value.z, result.z, 0.0001f);
        }

        [Test]
        public void ReadVector3Array_ReadsLengthAndValues() {
            BytesWriter writer = new BytesWriter();
            Vector3[] array = { new Vector3(1, 2, 3), new Vector3(4, 5, 6) };
            writer.WriteVector3Array(array);
            reader = new BytesReader(writer.Bytes);
            var result = reader.ReadVector3Array();
            Assert.AreEqual(array.Length, result.Length);
            for (int i = 0; i < array.Length; i++) {
                Assert.AreEqual(array[i].x, result[i].x, 0.0001f);
                Assert.AreEqual(array[i].y, result[i].y, 0.0001f);
                Assert.AreEqual(array[i].z, result[i].z, 0.0001f);
            }
        }

        [Test]
        public void ReadRect_ReadsCorrectValue() {
            BytesWriter writer = new BytesWriter();
            Rect value = new Rect(10, 20, 30, 40);
            writer.WriteRect(value);
            reader = new BytesReader(writer.Bytes);
            Rect result = reader.ReadRect();
            Assert.AreEqual(value.x, result.x, 0.0001f);
            Assert.AreEqual(value.y, result.y, 0.0001f);
            Assert.AreEqual(value.width, result.width, 0.0001f);
            Assert.AreEqual(value.height, result.height, 0.0001f);
        }

        [Test]
        public void ReadRectArray_ReadsLengthAndValues() {
            BytesWriter writer = new BytesWriter();
            Rect[] array = { new Rect(1, 2, 3, 4), new Rect(5, 6, 7, 8) };
            writer.WriteRectArray(array);
            reader = new BytesReader(writer.Bytes);
            var result = reader.ReadRectArray();
            Assert.AreEqual(array.Length, result.Length);
            for (int i = 0; i < array.Length; i++) {
                Assert.AreEqual(array[i].x, result[i].x, 0.0001f);
                Assert.AreEqual(array[i].y, result[i].y, 0.0001f);
                Assert.AreEqual(array[i].width, result[i].width, 0.0001f);
                Assert.AreEqual(array[i].height, result[i].height, 0.0001f);
            }
        }

        [Test]
        public void ReadColor32_ReadsCorrectValue() {
            BytesWriter writer = new BytesWriter();
            Color32 value = new Color32(100, 150, 200, 255);
            writer.WriteColor32(value);
            reader = new BytesReader(writer.Bytes);
            Color32 result = reader.ReadColor32();
            Assert.AreEqual(value.r, result.r);
            Assert.AreEqual(value.g, result.g);
            Assert.AreEqual(value.b, result.b);
            Assert.AreEqual(value.a, result.a);
        }

        [Test]
        public void ReadColor32Array_ReadsLengthAndValues() {
            BytesWriter writer = new BytesWriter();
            Color32[] array = { new Color32(100, 150, 200, 255), new Color32(50, 75, 100, 128) };
            writer.WriteColor32Array(array);
            reader = new BytesReader(writer.Bytes);
            var result = reader.ReadColor32Array();
            Assert.AreEqual(array.Length, result.Length);
            for (int i = 0; i < array.Length; i++) {
                Assert.AreEqual(array[i].r, result[i].r);
                Assert.AreEqual(array[i].g, result[i].g);
                Assert.AreEqual(array[i].b, result[i].b);
                Assert.AreEqual(array[i].a, result[i].a);
            }
        }

        [Test]
        public void ReadColor_ReadsCorrectValue() {
            BytesWriter writer = new BytesWriter();
            Color value = new Color(0.5f, 0.6f, 0.7f, 1.0f);
            writer.WriteColor(value);
            reader = new BytesReader(writer.Bytes);
            Color result = reader.ReadColor();
            Assert.AreEqual(value.r, result.r, 0.0001f);
            Assert.AreEqual(value.g, result.g, 0.0001f);
            Assert.AreEqual(value.b, result.b, 0.0001f);
            Assert.AreEqual(value.a, result.a, 0.0001f);
        }

        [Test]
        public void ReadColorArray_ReadsLengthAndValues() {
            BytesWriter writer = new BytesWriter();
            Color[] array = { new Color(0.1f, 0.2f, 0.3f, 1.0f), new Color(0.4f, 0.5f, 0.6f, 0.5f) };
            writer.WriteColorArray(array);
            reader = new BytesReader(writer.Bytes);
            var result = reader.ReadColorArray();
            Assert.AreEqual(array.Length, result.Length);
            for (int i = 0; i < array.Length; i++) {
                Assert.AreEqual(array[i].r, result[i].r, 0.0001f);
                Assert.AreEqual(array[i].g, result[i].g, 0.0001f);
                Assert.AreEqual(array[i].b, result[i].b, 0.0001f);
                Assert.AreEqual(array[i].a, result[i].a, 0.0001f);
            }
        }

        [Test]
        public void Cursor_InitializesToZero() {
            byte[] data = { 1, 2, 3 };
            reader = new BytesReader(data);
            Assert.AreEqual(0, reader.Cursor);
        }

        [Test]
        public void Cursor_AdvancesAfterRead() {
            byte[] data = { 1, 2, 3, 4 };
            reader = new BytesReader(data);
            reader.ReadInt();
            Assert.AreEqual(4, reader.Cursor);
        }

        [Test]
        public void ReadMultipleTypes_ReadsInCorrectOrder() {
            BytesWriter writer = new BytesWriter();
            writer.WriteByte(10)
                  .WriteShort(20)
                  .WriteInt(30)
                  .WriteFloat(40.5f)
                  .WriteString("Test");
            reader = new BytesReader(writer.Bytes);
            bool success = reader.ReadByte(out byte byteValue);
            Assert.IsTrue(success);
            Assert.AreEqual(10, byteValue);
            Assert.AreEqual(20, reader.ReadShort());
            Assert.AreEqual(30, reader.ReadInt());
            Assert.AreEqual(40.5f, reader.ReadFloat(), 0.0001f);
            Assert.AreEqual("Test", reader.ReadString());
        }

        [Test]
        public void ReadEmptyArray_HandlesEmptyArrays() {
            BytesWriter writer = new BytesWriter();
            Int32[] emptyArray = { };
            writer.WriteIntArray(emptyArray);
            reader = new BytesReader(writer.Bytes);
            var result = reader.ReadIntArray();
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void ReadEmptyString_HandlesEmptyString() {
            BytesWriter writer = new BytesWriter();
            writer.WriteString("");
            reader = new BytesReader(writer.Bytes);
            Assert.AreEqual("", reader.ReadString());
        }

        [Test]
        public void ReadNegativeValues_HandlesNegativeNumbers() {
            BytesWriter writer = new BytesWriter();
            writer.WriteInt(-12345)
                  .WriteShort(-1234)
                  .WriteLong(-1234567890L);
            reader = new BytesReader(writer.Bytes);
            Assert.AreEqual(-12345, reader.ReadInt());
            Assert.AreEqual(-1234, reader.ReadShort());
            Assert.AreEqual(-1234567890L, reader.ReadLong());
        }
    }
}

