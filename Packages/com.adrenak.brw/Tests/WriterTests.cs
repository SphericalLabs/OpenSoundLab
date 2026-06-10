using System;
using NUnit.Framework;
using UnityEngine;

namespace Adrenak.BRW.Tests {
    public class WriterTests {
        BytesWriter writer;

        [SetUp]
        public void SetUp() {
            writer = new BytesWriter();
        }

        [Test]
        public void WriteByte_AddsByteToBytes() {
            writer.WriteByte(42);
            var bytes = writer.Bytes;
            Assert.AreEqual(1, bytes.Length);
            Assert.AreEqual(42, bytes[0]);
        }

        [Test]
        public void WriteBytes_AddsByteArrayToBytes() {
            byte[] data = { 1, 2, 3, 4 };
            writer.WriteBytes(data);
            var bytes = writer.Bytes;
            Assert.AreEqual(4, bytes.Length);
            Assert.AreEqual(data, bytes);
        }

        [Test]
        public void WriteByteArray_WritesLengthAndBytes() {
            byte[] data = { 10, 20, 30 };
            writer.WriteByteArray(data);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            int length = reader.ReadInt();
            Assert.AreEqual(3, length);
            var readData = reader.ReadBytes(3);
            Assert.AreEqual(data, readData);
        }

        [Test]
        public void WriteShort_WritesCorrectValue() {
            Int16 value = 12345;
            writer.WriteShort(value);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            Assert.AreEqual(value, reader.ReadShort());
        }

        [Test]
        public void WriteShortArray_WritesLengthAndValues() {
            Int16[] array = { 1, 2, 3, 4, 5 };
            writer.WriteShortArray(array);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            var readArray = reader.ReadShortArray();
            Assert.AreEqual(array, readArray);
        }

        [Test]
        public void WriteInt_WritesCorrectValue() {
            Int32 value = 123456789;
            writer.WriteInt(value);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            Assert.AreEqual(value, reader.ReadInt());
        }

        [Test]
        public void WriteIntArray_WritesLengthAndValues() {
            Int32[] array = { 100, 200, 300 };
            writer.WriteIntArray(array);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            var readArray = reader.ReadIntArray();
            Assert.AreEqual(array, readArray);
        }

        [Test]
        public void WriteLong_WritesCorrectValue() {
            Int64 value = 123456789012345;
            writer.WriteLong(value);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            Assert.AreEqual(value, reader.ReadLong());
        }

        [Test]
        public void WriteLongArray_WritesLengthAndValues() {
            Int64[] array = { 1000L, 2000L, 3000L };
            writer.WriteLongArray(array);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            var readArray = reader.ReadLongArray();
            Assert.AreEqual(array, readArray);
        }

        [Test]
        public void WriteFloat_WritesCorrectValue() {
            float value = 3.14159f;
            writer.WriteFloat(value);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            Assert.AreEqual(value, reader.ReadFloat(), 0.0001f);
        }

        [Test]
        public void WriteFloatArray_WritesLengthAndValues() {
            float[] array = { 1.1f, 2.2f, 3.3f };
            writer.WriteFloatArray(array);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            var readArray = reader.ReadFloatArray();
            Assert.AreEqual(array.Length, readArray.Length);
            for (int i = 0; i < array.Length; i++) {
                Assert.AreEqual(array[i], readArray[i], 0.0001f);
            }
        }

        [Test]
        public void WriteDouble_WritesCorrectValue() {
            double value = 3.141592653589793;
            writer.WriteDouble(value);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            Assert.AreEqual(value, reader.ReadDouble(), 0.0000001);
        }

        [Test]
        public void WriteDoubleArray_WritesLengthAndValues() {
            double[] array = { 1.1, 2.2, 3.3 };
            writer.WriteDoubleArray(array);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            var readArray = reader.ReadDoubleArray();
            Assert.AreEqual(array.Length, readArray.Length);
            for (int i = 0; i < array.Length; i++) {
                Assert.AreEqual(array[i], readArray[i], 0.0000001);
            }
        }

        [Test]
        public void WriteChar_WritesCorrectValue() {
            char value = 'A';
            writer.WriteChar(value);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            Assert.AreEqual(value, reader.ReadChar());
        }

        [Test]
        public void WriteString_WritesLengthAndUTF8Bytes() {
            string value = "Hello, World!";
            writer.WriteString(value);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            Assert.AreEqual(value, reader.ReadString());
        }

        [Test]
        public void WriteStringArray_WritesLengthAndStrings() {
            string[] array = { "Hello", "World", "Test" };
            writer.WriteStringArray(array);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            var readArray = reader.ReadStringArray();
            Assert.AreEqual(array, readArray);
        }

        [Test]
        public void WriteVector2_WritesCorrectValue() {
            Vector2 value = new Vector2(1.5f, 2.5f);
            writer.WriteVector2(value);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            Vector2 readValue = reader.ReadVector2();
            Assert.AreEqual(value.x, readValue.x, 0.0001f);
            Assert.AreEqual(value.y, readValue.y, 0.0001f);
        }

        [Test]
        public void WriteVector2Array_WritesLengthAndValues() {
            Vector2[] array = { new Vector2(1, 2), new Vector2(3, 4) };
            writer.WriteVector2Array(array);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            var readArray = reader.ReadVector2Array();
            Assert.AreEqual(array.Length, readArray.Length);
            for (int i = 0; i < array.Length; i++) {
                Assert.AreEqual(array[i].x, readArray[i].x, 0.0001f);
                Assert.AreEqual(array[i].y, readArray[i].y, 0.0001f);
            }
        }

        [Test]
        public void WriteVector3_WritesCorrectValue() {
            Vector3 value = new Vector3(1.5f, 2.5f, 3.5f);
            writer.WriteVector3(value);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            Vector3 readValue = reader.ReadVector3();
            Assert.AreEqual(value.x, readValue.x, 0.0001f);
            Assert.AreEqual(value.y, readValue.y, 0.0001f);
            Assert.AreEqual(value.z, readValue.z, 0.0001f);
        }

        [Test]
        public void WriteVector3Array_WritesLengthAndValues() {
            Vector3[] array = { new Vector3(1, 2, 3), new Vector3(4, 5, 6) };
            writer.WriteVector3Array(array);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            var readArray = reader.ReadVector3Array();
            Assert.AreEqual(array.Length, readArray.Length);
            for (int i = 0; i < array.Length; i++) {
                Assert.AreEqual(array[i].x, readArray[i].x, 0.0001f);
                Assert.AreEqual(array[i].y, readArray[i].y, 0.0001f);
                Assert.AreEqual(array[i].z, readArray[i].z, 0.0001f);
            }
        }

        [Test]
        public void WriteRect_WritesCorrectValue() {
            Rect value = new Rect(10, 20, 30, 40);
            writer.WriteRect(value);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            Rect readValue = reader.ReadRect();
            Assert.AreEqual(value.x, readValue.x, 0.0001f);
            Assert.AreEqual(value.y, readValue.y, 0.0001f);
            Assert.AreEqual(value.width, readValue.width, 0.0001f);
            Assert.AreEqual(value.height, readValue.height, 0.0001f);
        }

        [Test]
        public void WriteRectArray_WritesLengthAndValues() {
            Rect[] array = { new Rect(1, 2, 3, 4), new Rect(5, 6, 7, 8) };
            writer.WriteRectArray(array);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            var readArray = reader.ReadRectArray();
            Assert.AreEqual(array.Length, readArray.Length);
            for (int i = 0; i < array.Length; i++) {
                Assert.AreEqual(array[i].x, readArray[i].x, 0.0001f);
                Assert.AreEqual(array[i].y, readArray[i].y, 0.0001f);
                Assert.AreEqual(array[i].width, readArray[i].width, 0.0001f);
                Assert.AreEqual(array[i].height, readArray[i].height, 0.0001f);
            }
        }

        [Test]
        public void WriteColor32_WritesCorrectValue() {
            Color32 value = new Color32(100, 150, 200, 255);
            writer.WriteColor32(value);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            Color32 readValue = reader.ReadColor32();
            Assert.AreEqual(value.r, readValue.r);
            Assert.AreEqual(value.g, readValue.g);
            Assert.AreEqual(value.b, readValue.b);
            Assert.AreEqual(value.a, readValue.a);
        }

        [Test]
        public void WriteColor32Array_WritesLengthAndValues() {
            Color32[] array = { new Color32(100, 150, 200, 255), new Color32(50, 75, 100, 128) };
            writer.WriteColor32Array(array);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            var readArray = reader.ReadColor32Array();
            Assert.AreEqual(array.Length, readArray.Length);
            for (int i = 0; i < array.Length; i++) {
                Assert.AreEqual(array[i].r, readArray[i].r);
                Assert.AreEqual(array[i].g, readArray[i].g);
                Assert.AreEqual(array[i].b, readArray[i].b);
                Assert.AreEqual(array[i].a, readArray[i].a);
            }
        }

        [Test]
        public void WriteColor_WritesCorrectValue() {
            Color value = new Color(0.5f, 0.6f, 0.7f, 1.0f);
            writer.WriteColor(value);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            Color readValue = reader.ReadColor();
            Assert.AreEqual(value.r, readValue.r, 0.0001f);
            Assert.AreEqual(value.g, readValue.g, 0.0001f);
            Assert.AreEqual(value.b, readValue.b, 0.0001f);
            Assert.AreEqual(value.a, readValue.a, 0.0001f);
        }

        [Test]
        public void WriteColorArray_WritesLengthAndValues() {
            Color[] array = { new Color(0.1f, 0.2f, 0.3f, 1.0f), new Color(0.4f, 0.5f, 0.6f, 0.5f) };
            writer.WriteColorArray(array);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            var readArray = reader.ReadColorArray();
            Assert.AreEqual(array.Length, readArray.Length);
            for (int i = 0; i < array.Length; i++) {
                Assert.AreEqual(array[i].r, readArray[i].r, 0.0001f);
                Assert.AreEqual(array[i].g, readArray[i].g, 0.0001f);
                Assert.AreEqual(array[i].b, readArray[i].b, 0.0001f);
                Assert.AreEqual(array[i].a, readArray[i].a, 0.0001f);
            }
        }

        [Test]
        public void OverwriteByte_OverwritesByteAtIndex() {
            writer.WriteInt(0);
            writer.OverwriteByte(0, 255);
            var bytes = writer.Bytes;
            Assert.AreEqual(255, bytes[0]);
        }

        [Test]
        public void OverwriteBytes_OverwritesBytesAtIndex() {
            writer.WriteInt(0);
            writer.WriteInt(0);
            byte[] newBytes = { 1, 2, 3, 4 };
            writer.OverwriteBytes(0, newBytes);
            var bytes = writer.Bytes;
            for (int i = 0; i < newBytes.Length; i++) {
                Assert.AreEqual(newBytes[i], bytes[i]);
            }
        }

        [Test]
        public void MethodChaining_ReturnsWriterInstance() {
            var result = writer.WriteByte(1).WriteInt(2).WriteFloat(3.0f);
            Assert.AreEqual(writer, result);
        }

        [Test]
        public void WriteEmptyArray_HandlesEmptyArrays() {
            Int32[] emptyArray = { };
            writer.WriteIntArray(emptyArray);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            var readArray = reader.ReadIntArray();
            Assert.AreEqual(0, readArray.Length);
        }

        [Test]
        public void WriteEmptyString_HandlesEmptyString() {
            writer.WriteString("");
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            Assert.AreEqual("", reader.ReadString());
        }

        [Test]
        public void WriteNegativeValues_HandlesNegativeNumbers() {
            writer.WriteInt(-12345);
            writer.WriteShort(-1234);
            writer.WriteLong(-1234567890L);
            var bytes = writer.Bytes;
            var reader = new BytesReader(bytes);
            Assert.AreEqual(-12345, reader.ReadInt());
            Assert.AreEqual(-1234, reader.ReadShort());
            Assert.AreEqual(-1234567890L, reader.ReadLong());
        }
    }
}

