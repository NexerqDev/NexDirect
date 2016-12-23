﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexDirectLib.Structures
{
    /// <summary>
    /// Beatmap Set class
    /// </summary>
    public class BeatmapSet
    {
        public string Id { get; set; }
        public string Artist { get; set; }
        public string Title { get; set; }
        public string Mapper { get; set; }
        public string RankingStatus { get; set; }
        public IEnumerable<Difficulty> Difficulties { get; set; }
        public bool AlreadyHave { get; set; }
        public Uri PreviewImage { get; set; }
        public bool IsBloodcat { get; set; }
        public JObject RawData { get; set; }

        public BeatmapSet(string id, string artist, string title, string mapper, string rankStatus, Dictionary<string, string> difficulties, JObject rawData, bool isBloodcat = true)
        {
            Id = id;
            Artist = artist;
            Title = title;
            Mapper = mapper;
            RankingStatus = rankStatus;
            PreviewImage = new Uri($"http://b.ppy.sh/thumb/{Id}l.jpg");
            AlreadyHave = MapsManager.Maps.Any(b => b.Contains(Id + " "));
            Difficulties = difficulties.Select(d => new Difficulty(d.Key, d.Value));

            IsBloodcat = isBloodcat;
            RawData = rawData;
        }

        public class Difficulty
        {
            public string Name { get; set; }
            public string Mode { get; set; }
            public Uri ModeImage { get; set; }

            public Difficulty(string name, string mode)
            {
                Name = name;
                Mode = mode;

                string _image;
                switch (mode)
                {
                    case "1":
                        _image = "pack://application:,,,/Resources/mode-taiko-small.png"; break;
                    case "2":
                        _image = "pack://application:,,,/Resources/mode-fruits-small.png"; break;
                    case "3":
                        _image = "pack://application:,,,/Resources/mode-mania-small.png"; break;
                    default:
                        _image = "pack://application:,,,/Resources/mode-osu-small.png"; break;
                }
                ModeImage = new Uri(_image);
            }
        }
    }
}
