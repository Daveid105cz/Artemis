﻿using System;
using System.Collections.Generic;
using Artemis.Storage.Entities.Profile.Abstract;
using LiteDB;

namespace Artemis.Storage.Entities.Profile
{
    public class FolderEntity : EffectsEntity
    {
        public FolderEntity()
        {
            PropertyEntities = new List<PropertyEntity>();
            LayerEffects = new List<LayerEffectEntity>();
            ExpandedPropertyGroups = new List<string>();
        }

        public Guid Id { get; set; }
        public Guid ParentId { get; set; }

        public int Order { get; set; }
        public string Name { get; set; }
        public bool Enabled { get; set; }

        [BsonRef("ProfileEntity")]
        public ProfileEntity Profile { get; set; }

        public Guid ProfileId { get; set; }
    }
}