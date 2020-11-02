# the_new_normal.exe

See https://www.ludwigzeller.net/projects/the-new-normal/ for more information.

Forked from Logan Olson's magnificent [SoundStage VR](https://github.com/googlearchive/soundstagevr).

Models: 'Rubber Med Gloves' by Komodoz, 'surgical mask' by F2A, 'Safety Goggles' by bariacg.

--

## Notes for making new MenuItems copiable and save/loadable

- add / copy yourDeviceInterface.cs 
- define YourData, implement (de)serialization 

- add YourData to xmlUpdate.cs
- add [XmlInclude(typeof(YourData))] to SaveLoadInterface.cs
