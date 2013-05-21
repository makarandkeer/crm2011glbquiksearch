// =====================================================================
//
//  This file is part of the Microsoft Dynamics CRM SDK code samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft
//  Development Tools and/or on-line documentation.  See these other
//  materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//  PARTICULAR PURPOSE.
//
// =====================================================================
//<snippetSilverlightExtensionMethods>


using System;
using System.Windows.Browser;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ServiceModel.Description;

namespace GlobalAdvanceFind_SL.CrmSDK
{
    partial class Entity
    {
        public Entity()
        {
            this.FormattedValuesField = new FormattedValueCollection();
            this.RelatedEntitiesField = new RelatedEntityCollection();
        }

        public T GetAttributeValue<T>(string attributeLogicalName)
        {
            if (null == this.Attributes) { this.Attributes = new AttributeCollection(); };

            object value;
            if (this.Attributes.TryGetValue(attributeLogicalName, out value))
            {
                return (T)value;
            }

            return default(T);
        }

        public object this[string attributeName]
        {
            get
            {
                if (null == this.Attributes) { this.Attributes = new AttributeCollection(); };
                return this.Attributes.GetItem(attributeName);
            }

            set
            {
                if (null == this.Attributes) { this.Attributes = new AttributeCollection(); };
                this.Attributes.SetItem(attributeName, value);
            }
        }
    }

    [KnownType(typeof(AppointmentRequest))]
    [KnownType(typeof(AttributeMetadata))]
    [KnownType(typeof(ColumnSet))]
    [KnownType(typeof(DateTime))]
    [KnownType(typeof(Entity))]
    [KnownType(typeof(EntityCollection))]
    [KnownType(typeof(EntityFilters))]
    [KnownType(typeof(EntityMetadata))]
    [KnownType(typeof(EntityReference))]
    [KnownType(typeof(EntityReferenceCollection))]
    [KnownType(typeof(Label))]
    [KnownType(typeof(LookupAttributeMetadata))]
    [KnownType(typeof(ManyToManyRelationshipMetadata))]
    [KnownType(typeof(OneToManyRelationshipMetadata))]
    [KnownType(typeof(OptionSetMetadataBase))]
    [KnownType(typeof(OptionSetValue))]
    [KnownType(typeof(PagingInfo))]
    [KnownType(typeof(ParameterCollection))]
    [KnownType(typeof(PrincipalAccess))]
    [KnownType(typeof(PropagationOwnershipOptions))]
    [KnownType(typeof(QueryBase))]
    [KnownType(typeof(Relationship))]
    [KnownType(typeof(RelationshipMetadataBase))]
    [KnownType(typeof(RelationshipQueryCollection))]
    [KnownType(typeof(RibbonLocationFilters))]
    [KnownType(typeof(RollupType))]
    [KnownType(typeof(StringAttributeMetadata))]
    [KnownType(typeof(TargetFieldType))]
    partial class OrganizationRequest
    {
        public object this[string key]
        {
            get
            {
                if (null == this.Parameters) { this.Parameters = new ParameterCollection(); };

                return this.Parameters.GetItem(key);
            }

            set
            {
                if (null == this.Parameters) { this.Parameters = new ParameterCollection(); };

                this.Parameters.SetItem(key, value);
            }
        }
    }

    [KnownType(typeof(AccessRights))]
    [KnownType(typeof(AttributeMetadata))]
    [KnownType(typeof(AttributePrivilegeCollection))]
    [KnownType(typeof(AuditDetail))]
    [KnownType(typeof(AuditDetailCollection))]
    [KnownType(typeof(AuditPartitionDetailCollection))]
    [KnownType(typeof(DateTime))]
    [KnownType(typeof(Entity))]
    [KnownType(typeof(EntityCollection))]
    [KnownType(typeof(EntityMetadata))]
    [KnownType(typeof(EntityReferenceCollection))]
    [KnownType(typeof(Guid))]
    [KnownType(typeof(Label))]
    [KnownType(typeof(ManagedPropertyMetadata))]
    [KnownType(typeof(OptionSetMetadataBase))]
    [KnownType(typeof(OrganizationResources))]
    [KnownType(typeof(ParameterCollection))]
    [KnownType(typeof(QueryExpression))]
    [KnownType(typeof(RelationshipMetadataBase))]
    [KnownType(typeof(SearchResults))]
    [KnownType(typeof(ValidationResult))]
    partial class OrganizationResponse
    {
        public object this[string key]
        {
            get
            {
                if (null == this.Results) { this.Results = new ParameterCollection(); };

                return this.Results.GetItem(key);
            }
        }
    }

    public static class CollectionExtensions
    {
        public static TValue GetItem<TKey, TValue>(this IList<KeyValuePair<TKey, TValue>> collection, TKey key)
        {
            TValue value;
            if (TryGetValue(collection, key, out value))
            {
                return value;
            }

            throw new System.Collections.Generic.KeyNotFoundException("Key = " + key);
        }

        public static void SetItem<TKey, TValue>(this IList<KeyValuePair<TKey, TValue>> collection, TKey key, TValue value)
        {
            int index;
            if (TryGetIndex<TKey, TValue>(collection, key, out index))
            {
                collection.RemoveAt(index);
            }

            //If the value is an array, it needs to be converted into a List. This is due to how Silverlight serializes
            //Arrays and IList<T> objects (they are both serialized with the same namespace). Any collection objects will
            //already add the KnownType for IList<T>, which means that any parameters that are arrays cannot be added
            //as a KnownType (or it will throw an exception).
            Array array = value as Array;
            if (null != array)
            {
                Type listType = typeof(List<>).GetGenericTypeDefinition().MakeGenericType(array.GetType().GetElementType());
                object list = Activator.CreateInstance(listType, array);
                try
                {
                    value = (TValue)list;
                }
                catch (InvalidCastException)
                {
                    //Don't do the conversion because the types are not compatible
                }
            }

            collection.Add(new KeyValuePair<TKey, TValue>() { Key = key, Value = value });
        }

        public static bool ContainsKey<TKey, TValue>(this IList<KeyValuePair<TKey, TValue>> collection, TKey key)
        {
            int index;
            return TryGetIndex<TKey, TValue>(collection, key, out index);
        }

        public static bool TryGetValue<TKey, TValue>(this IList<KeyValuePair<TKey, TValue>> collection, TKey key, out TValue value)
        {
            int index;
            if (TryGetIndex<TKey, TValue>(collection, key, out index))
            {
                value = collection[index].Value;
                return true;
            }

            value = default(TValue);
            return false;
        }

        private static bool TryGetIndex<TKey, TValue>(IList<KeyValuePair<TKey, TValue>> collection, TKey key, out int index)
        {
            if (null == collection || null == key)
            {
                index = -1;
                return false;
            }

            index = -1;
            for (int i = 0; i < collection.Count; i++)
            {
                if (key.Equals(collection[i].Key))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }
    }

    [KnownType(typeof(QueryBase))]
    [KnownType(typeof(Relationship))]
    [KnownType(typeof(EntityCollection))]
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/System.Collections.Generic")]
    public sealed class KeyValuePair<TKey, TValue>
    {
        #region Properties
        [DataMember(Name = "key")]
        public TKey Key { get; set; }

        [DataMember(Name = "value")]
        public TValue Value { get; set; }
        #endregion
    }

    #region Collection Instantiation
    partial class EntityCollection
    {
        public EntityCollection()
        {
            this.EntitiesField = new ObservableCollection<Entity>();
        }
    }

    partial class Label
    {
        public Label()
        {
            this.LocalizedLabelsField = new LocalizedLabelCollection();
        }
    }

    partial class ColumnSet
    {
        public ColumnSet()
        {
            this.ColumnsField = new ObservableCollection<string>();
        }
    }

    partial class ConditionExpression
    {
        public ConditionExpression()
        {
            this.ValuesField = new ObservableCollection<object>();
        }
    }

    partial class FilterExpression
    {
        public FilterExpression()
        {
            this.ConditionsField = new ObservableCollection<ConditionExpression>();
            this.FiltersField = new ObservableCollection<FilterExpression>();
        }
    }

    partial class LinkEntity
    {
        public LinkEntity()
        {
            this.LinkEntitiesField = new ObservableCollection<LinkEntity>();
        }
    }

    partial class QueryByAttribute
    {
        public QueryByAttribute()
        {
            this.AttributesField = new ObservableCollection<string>();
            this.ValuesField = new ObservableCollection<object>();
            this.OrdersField = new ObservableCollection<OrderExpression>();
        }
    }

    partial class QueryExpression
    {
        public QueryExpression()
        {
            this.LinkEntitiesField = new ObservableCollection<LinkEntity>();
            this.OrdersField = new ObservableCollection<OrderExpression>();
        }
    }

    partial class OptionSetMetadata
    {
        public OptionSetMetadata()
        {
            this.OptionsField = new ObservableCollection<OptionMetadata>();
        }
    }
    #endregion
}
//</snippetSilverlightExtensionMethods>
