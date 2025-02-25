﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
#if !SILVERLIGHT
using System.Windows.Forms;
#endif

namespace Rawr;

public class ItemFilterRegexList :
#if SILVERLIGHT
    List<ItemFilterRegex>
#else
    BindingList<ItemFilterRegex>
#endif
{
}

public class ItemFilterEnabledOverride
{
    public bool Enabled { get; set; }
    public string Name { get; set; }
    public List<ItemFilterEnabledOverride> SubFilterOverride { get; set; }
}

public class ItemFilterRegex
{
    private static bool regexCompiled;
    public bool AdditiveFilter { get; set; }
    public bool AppliesToGems { get; set; }
    public bool AppliesToItems { get; set; }

    public bool BoA { get; set; }
    public bool BoE { get; set; }
    public bool BoN { get; set; }
    public bool BoP { get; set; }
    public bool BoU { get; set; }

    public bool Enabled { get; set; }
    public int MaxItemLevel { get; set; }
    public ItemQuality MaxItemQuality { get; set; }

    public int MinItemLevel { get; set; }
    public ItemQuality MinItemQuality { get; set; }

    public string Name { get; set; }

    public string Pattern
    {
        get => _pattern;
        set
        {
            _pattern = value;
            _regex = null;
        }
    }

    [XmlIgnore]
    public Regex Regex
    {
        get
        {
            if (_regex == null)
            {
                if (_pattern == null) _pattern = "";
                var options = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline;
#if !SILVERLIGHT
                if (RegexCompiled) options |= RegexOptions.Compiled;
#endif
                _regex = new Regex(_pattern, options);
            }

            return _regex;
        }
    }

    public static bool RegexCompiled
    {
        get { return regexCompiled; }
        set
        {
            if (regexCompiled != value)
            {
                regexCompiled = value;
#if !SILVERLIGHT
                if (ItemFilter.FilterList != null)
                    foreach (var regex in ItemFilter.FilterList)
                        regex.ResetRegex();
#endif
            }
        }
    }

    [XmlIgnore] private string _pattern;

    public ItemFilterRegexList RegexList = new();
    public bool OtherRegexEnabled = true;

    [XmlIgnore] private Regex _regex;

    public ItemFilterRegex()
    {
        Name = "New Filter";
        Enabled = true;
        MinItemLevel = 0;
        MaxItemLevel = 1000;
        MinItemQuality = ItemQuality.Temp;
        MaxItemQuality = ItemQuality.Heirloom;
        BoA = true;
        BoE = true;
        BoP = true;
        BoU = true;
        BoN = true;
        AdditiveFilter = true;
        AppliesToItems = true;
        AppliesToGems = true;
    }

    public bool IsMatch(Item item)
    {
        // all checks must pass for match to pass, so do the fast checks first
        if (item.ItemLevel >= MinItemLevel && item.ItemLevel <= MaxItemLevel)
            if (item.Quality >= MinItemQuality && item.Quality <= MaxItemQuality)
            {
                var locationInfo = item.LocationInfo;
                if (string.IsNullOrEmpty(_pattern)
                    || RegexIsMatch(locationInfo[0].Description)
                    || RegexIsMatch(locationInfo[0].Note)
                    //|| (locationInfo[1] != null && !string.IsNullOrEmpty(locationInfo[1].Description) && Regex.IsMatch(locationInfo[1].Description))
                    || (locationInfo[0] != null && locationInfo[1] != null &&
                        !string.IsNullOrEmpty(locationInfo[0].Description) &&
                        !string.IsNullOrEmpty(locationInfo[1].Description) && Regex.IsMatch(
                            locationInfo[0].Description + " and" +
                            locationInfo[1].Description.Replace("Purchasable with", "")))
                   )
                {
                    // no bind specified on the filter - we fine
                    /*if (!(BoA || BoE || BoP || BoU || BoN)) return true;
                    // item doesn't have real bind data, force it to show
                    else*/
                    if (!BoN && !AdditiveFilter && item.Bind == BindsOn.None) return true;
                    // There's Bind data set on the filter and Item has valid bind data
                    if (BoA && item.Bind == BindsOn.BoA) return true;
                    if (BoE && item.Bind == BindsOn.BoE) return true;
                    if (BoP && item.Bind == BindsOn.BoP) return true;
                    if (BoU && item.Bind == BindsOn.BoU) return true;
                    if (BoN && item.Bind == BindsOn.None) return true;
                }
            }

        return false;
    }

    public bool AppliesTo(Item item)
    {
        return (item.IsGem && AppliesToGems) || (!item.IsGem && AppliesToItems);
    }

    public bool IsEnabled(Item item)
    {
        if (!Enabled) return false;
        if (RegexList.Count == 0) return true;
        var enabledMatch = false;
        var anyMatch = false;
        foreach (var regex in RegexList)
            if (regex.AppliesTo(item) && regex.IsMatch(item))
            {
                anyMatch = true;
                if (regex.IsEnabled(item))
                {
                    enabledMatch = true;
                    break;
                }
            }

        if (anyMatch)
            return enabledMatch;
        return OtherRegexEnabled;
    }

    private void ResetRegex()
    {
        _regex = null;
        foreach (var regex in RegexList) regex.ResetRegex();
    }

    private bool RegexIsMatch(string text)
    {
        return !string.IsNullOrEmpty(text) && Regex.IsMatch(text);
    }
}

[GenerateSerializer]
public class ItemTypeList : List<ItemType>
{
    public ItemTypeList()
    {
    }

    public ItemTypeList(IEnumerable<ItemType> collection) : base(collection)
    {
    }
}

[GenerateSerializer]
public class ItemFilterData
{
    public SerializableDictionary<string, ItemTypeList> RelevantItemTypes = new();
    public ItemFilterRegexList RegexList = new();
    public bool OtherRegexEnabled = true;
}

public static class ItemFilter
{
    private static ItemFilterData data = new();

    /// <summary>
    ///     Returns a list that can be used to set relevant item types list. Call ItemCache.OnItemsChanged()
    ///     when you finish making changes to the list.
    /// </summary>
    public static List<ItemType> GetRelevantItemTypesList(CalculationsBase model)
    {
        ItemTypeList list;
        if (!data.RelevantItemTypes.TryGetValue(model.Name, out list))
        {
            list = new ItemTypeList(model.RelevantItemTypes);
            data.RelevantItemTypes[model.Name] = list;
        }

        return list;
    }

    public static ItemFilterRegexList FilterList => data.RegexList;

    public static bool OtherEnabled
    {
        get => data.OtherRegexEnabled;
        set => data.OtherRegexEnabled = value;
    }

    public static bool IsItemRelevant(CalculationsBase model, Item item)
    {
        if (GetRelevantItemTypesList(model).Contains(item.Type))
        {
            var added = false;
            var anyMatch = false;
            var enabledMatch = false;
            foreach (var regex in data.RegexList)
                if (regex.AdditiveFilter && regex.AppliesTo(item) && regex.IsMatch(item))
                {
                    anyMatch = true;
                    if (regex.IsEnabled(item))
                    {
                        enabledMatch = true;
                        break;
                    }
                }

            if (anyMatch)
                added = enabledMatch;
            else
                added = OtherEnabled;
            if (added)
            {
                foreach (var regex in data.RegexList)
                    if (!regex.AdditiveFilter && regex.Enabled && regex.AppliesTo(item) && regex.IsMatch(item) &&
                        regex.IsEnabled(item))
                        return false;
                return true;
            }
        }

        return false;
    }

    public static bool IsLoading { get; set; }

#if SILVERLIGHT
        public static void Save(TextWriter writer)
        {
            System.Xml.Serialization.XmlSerializer serializer =
 new System.Xml.Serialization.XmlSerializer(typeof(ItemFilterData));
            serializer.Serialize(writer, data);
            writer.Close();
        }

        public static void Load(TextReader reader)
        {
            try
            {
                System.Xml.Serialization.XmlSerializer serializer =
 new System.Xml.Serialization.XmlSerializer(typeof(ItemFilterData));
                data = (ItemFilterData)serializer.Deserialize(reader);
                reader.Close();
            }
            catch (Exception)
            {
                data = new ItemFilterData();
            }
        }
#else
    public static void Compile()
    {
        //Console.WriteLine("Compile started");
        // the only way to get full jit is to exercise all code paths which will only
        // happen on actual data, so simulate item cache reset
        ItemCache.Instance.GetRelevantItemsInternal(Calculations.Instance);
        //Console.WriteLine("Compile ended");
    }

    public static void Save(string fileName)
    {
        using (var writer = new StreamWriter(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), fileName),
                   false, Encoding.UTF8))
        {
            var serializer = new XmlSerializer(typeof(ItemFilterData));
            serializer.Serialize(writer, data);
            writer.Close();
        }
    }

    public static void Load(string fileName)
    {
        if (File.Exists(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), fileName)))
            try
            {
                var xml = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), fileName));
                var reader = new StringReader(xml);
                var serializer = new XmlSerializer(typeof(ItemFilterData));
                data = (ItemFilterData)serializer.Deserialize(reader);
                reader.Close();
            }
            catch (Exception)
            {
                data = new ItemFilterData();
            }
        else
            data = new ItemFilterData();
    }
#endif
}