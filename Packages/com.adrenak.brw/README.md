## BRW

Binary Reader and Writer.

A tiny library to write C# in-built types and some Unity types into a byte array. BRW is used in [UniVoice](https://github.com/adrenak/univoice), the open source VoIP/voice chat plugin for Unity.

Allows for a fluent interface for writing like:
```
BytesWriter writer = new BytesWriter();
writer.WriteByte(10)
    .WriteShort(20)
    .WriteInt(30)
    .WriteFloat(40.5f)
    .WriteString("Test");
var writtenBytes = writer.Bytes;
```

While reading, readin the same order you wrote in:
```
BytesReader reader = new BytesReader(writtenBytes);
var myByte = reader.ReadByte();
var myShort = reader.ReadShort();
var myInt = reader.ReadInt();
var myFloat = reader.ReadFloat();
var myString = reader.ReadString();
```

## Installation
Install using NPM, ensure the following is present in your manifest.json
```
"scopedRegistries": [
    {
        "name": "npmjs",
        "url": "https://registry.npmjs.org",
        "scopes": [
            "com.npmjs",
            "com.adrenak.brw"
        ]
    }
]
```

## Contact  
[@github](https://www.github.com/adrenak)  
[@www](http://www.vatsalambastha.com)  