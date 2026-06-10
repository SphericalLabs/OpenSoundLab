using System.Collections.Generic;
using System.Linq;

public class DeviceType
{
    private static readonly List<DeviceType> instances = new List<DeviceType>();

    public string Name { get; set; }
    public DeviceCategory Category { get; set; }
    public int Order { get; set; }
    public bool IsAddon { get; private set; }
    public string AddonId { get; private set; }

    public DeviceType() { }

    private DeviceType(string name, DeviceCategory category, int orderWithinCategory, bool isAddon = false, string addonId = "")
    {
        Update(name, category, orderWithinCategory, isAddon, addonId);
        instances.Add(this);
    }

    void Update(string name, DeviceCategory category, int orderWithinCategory, bool isAddon = false, string addonId = "")
    {
        Name = name;
        Category = category;
        Order = orderWithinCategory;
        IsAddon = isAddon;
        AddonId = addonId;
    }

    public static DeviceType Register(string name, DeviceCategory category, int orderWithinCategory, bool isAddon = false, string addonId = "")
    {
        if (TryGet(name, out DeviceType existingType))
        {
            existingType.Update(name, category, orderWithinCategory, isAddon, addonId);
            return existingType;
        }

        return new DeviceType(name, category, orderWithinCategory, isAddon, addonId);
    }

    public static bool TryGet(string name, out DeviceType deviceType)
    {
        for (int i = 0; i < instances.Count; ++i)
        {
            if (instances[i].Name == name)
            {
                deviceType = instances[i];
                return true;
            }
        }

        deviceType = null;
        return false;
    }

    public static DeviceType Get(string name)
    {
        if (TryGet(name, out DeviceType deviceType)) return deviceType;
        if (OSLDeviceRegistry.TryGet(name, out OSLDeviceRegistration registration) && registration.deviceType != null) return registration.deviceType;
        return Register(name, DeviceCategory.Various, int.MaxValue);
    }

    public static IEnumerable<DeviceType> GetAll(bool sortAlphabetically = false)
    {
        return OSLDeviceRegistry.GetAll(sortAlphabetically)
            .Where(registration => registration != null && registration.deviceType != null && registration.IsAvailable)
            .Select(registration => registration.deviceType)
            .ToList();
    }

    public static IEnumerable<DeviceType> GetAllByCategory(DeviceCategory category, bool sortAlphabetically = false)
    {
        return OSLDeviceRegistry.GetAllByCategory(category, sortAlphabetically)
            .Where(registration => registration != null && registration.deviceType != null && registration.IsAvailable)
            .Select(registration => registration.deviceType)
            .ToList();
    }

    public static implicit operator string(DeviceType deviceType) => deviceType.Name;

    public override string ToString() => Name;
}
