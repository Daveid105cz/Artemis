﻿using Artemis.Models.Interfaces;
using Artemis.Models.Profiles.Properties;
using Artemis.Utilities;

namespace Artemis.ViewModels.LayerEditor
{
    public class HeadsetPropertiesViewModel : LayerPropertiesViewModel
    {
        private LayerPropertiesModel _proposedProperties;

        public HeadsetPropertiesViewModel(IGameDataModel gameDataModel, LayerPropertiesModel properties)
            : base(gameDataModel)
        {
            ProposedProperties = GeneralHelpers.Clone(properties);
        }

        public LayerPropertiesModel ProposedProperties
        {
            get { return _proposedProperties; }
            set
            {
                if (Equals(value, _proposedProperties)) return;
                _proposedProperties = value;
                NotifyOfPropertyChange(() => ProposedProperties);
            }
        }

        public override LayerPropertiesModel GetAppliedProperties()
        {
            return GeneralHelpers.Clone(ProposedProperties);
        }
    }
}