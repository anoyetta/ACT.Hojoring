using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using ACT.SpecialSpellTimer.Utility;

namespace ACT.SpecialSpellTimer.RaidTimeline
{
    public partial class TimelineModel
    {
        /// <summary>
        /// 規定の既定値の定義リスト
        /// </summary>
        private static readonly IList<TimelineDefaultModel> SuperDefaultValues = new[]
        {
            // Activity
            NewDefault(TimelineElementTypes.Activity, "Enabled", true),
            NewDefault(TimelineElementTypes.Activity, "SyncOffsetStart", -12d),
            NewDefault(TimelineElementTypes.Activity, "SyncOffsetEnd", 12d),
            NewDefault(TimelineElementTypes.Activity, "NoticeDevice", NoticeDevices.Both),
            NewDefault(TimelineElementTypes.Activity, "NoticeOffset", -6d),
            NewDefault(TimelineElementTypes.Activity, "NoticeSync", false),

            // Trigger
            NewDefault(TimelineElementTypes.Trigger, "No", 0),
            NewDefault(TimelineElementTypes.Trigger, "Enabled", true),
            NewDefault(TimelineElementTypes.Trigger, "SyncCount", 0),
            NewDefault(TimelineElementTypes.Trigger, "SyncInterval", 0),
            NewDefault(TimelineElementTypes.Trigger, "NoticeDevice", NoticeDevices.Both),
            NewDefault(TimelineElementTypes.Trigger, "NoticeSync", false),
            NewDefault(TimelineElementTypes.Trigger, "IsExecuteHidden", false),

            // Subroutine
            NewDefault(TimelineElementTypes.Subroutine, "Enabled", true),

            // Load
            NewDefault(TimelineElementTypes.Load, "Enabled", true),

            // VisualNotice
            NewDefault(TimelineElementTypes.VisualNotice, "Enabled", true),
            NewDefault(TimelineElementTypes.VisualNotice, "Text", TimelineVisualNoticeModel.ParentTextPlaceholder),
            NewDefault(TimelineElementTypes.VisualNotice, "Duration", 3d),
            NewDefault(TimelineElementTypes.VisualNotice, "DurationVisible", true),
            NewDefault(TimelineElementTypes.VisualNotice, "StackVisible", false),
            NewDefault(TimelineElementTypes.VisualNotice, "Order", 0),
            NewDefault(TimelineElementTypes.VisualNotice, "IsJobIcon", false),

            // ImageNotice
            NewDefault(TimelineElementTypes.ImageNotice, "Enabled", true),
            NewDefault(TimelineElementTypes.ImageNotice, "Duration", 5d),
            NewDefault(TimelineElementTypes.ImageNotice, "Scale", 1.0d),
            NewDefault(TimelineElementTypes.ImageNotice, "Left", -1d),
            NewDefault(TimelineElementTypes.ImageNotice, "Top", -1d),

            // P-Sync
            NewDefault(TimelineElementTypes.PositionSync, "Enabled", true),
            NewDefault(TimelineElementTypes.PositionSync, "Interval", 30d),

            // P-Sync - Combatant
            NewDefault(TimelineElementTypes.Combatant, "Enabled", true),
            NewDefault(TimelineElementTypes.Combatant, "X", TimelineCombatantModel.InvalidPosition),
            NewDefault(TimelineElementTypes.Combatant, "Y", TimelineCombatantModel.InvalidPosition),
            NewDefault(TimelineElementTypes.Combatant, "Z", TimelineCombatantModel.InvalidPosition),
            NewDefault(TimelineElementTypes.Combatant, "Tolerance", 0.01f),

            // Expressions
            NewDefault(TimelineElementTypes.Expressions, "Enabled", true),
            NewDefault(TimelineElementTypes.ExpressionsSet, "Enabled", true),
            NewDefault(TimelineElementTypes.ExpressionsSet, "Value", true),
            NewDefault(TimelineElementTypes.ExpressionsSet, "IsToggle", false),
            NewDefault(TimelineElementTypes.ExpressionsSet, "TTL", -1),
            NewDefault(TimelineElementTypes.ExpressionsPredicate, "Enabled", true),
            NewDefault(TimelineElementTypes.ExpressionsPredicate, "Value", true),

            // Import
            NewDefault(TimelineElementTypes.Import, "Enabled", true),
        };

        private void SetDefaultValues()
        {
            var defaults = this.Defaults.Union(SuperDefaultValues)
                .Where(x => (x.Enabled ?? true));

            this.Walk((element) => setDefaultValuesToElement(element));

            void setDefaultValuesToElement(TimelineBase element)
            {
                try
                {
                    foreach (var def in defaults
                        .Where(x => x.TargetElement == element.TimelineType))
                    {
                        var pi = GetPropertyInfo(element, def.TargetAttribute);
                        if (pi == null)
                        {
                            continue;
                        }

                        var value = pi.GetValue(element);
                        if (value == null)
                        {
                            object defValue = null;

                            if (def.Value != null)
                            {
                                var type = pi.PropertyType;

                                if (type.IsGenericType &&
                                    type.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    type = Nullable.GetUnderlyingType(type);
                                }

                                if (!type.IsEnum)
                                {
                                    defValue = Convert.ChangeType(def.Value, type);
                                }
                                else
                                {
                                    defValue = Enum.Parse(type, def.Value, true);
                                }
                            }

                            if (defValue != null)
                            {
                                pi.SetValue(element, defValue);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Write("[TL] Load default values error.", ex);
                }
            }
        }

        private static PropertyInfo GetPropertyInfo(
            TimelineBase element,
            string fieldName)
        {
            const BindingFlags flag =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.IgnoreCase;

            var info = default(PropertyInfo);

            var type = element.GetType();
            if (type == null)
            {
                return info;
            }

            // 通常のフィールド名からフィールド情報を取得する
            info = type.GetProperty(fieldName, flag);

            if (info != null)
            {
                return info;
            }

            // XML属性名からフィールド情報を取得する
            var pis = type.GetProperties(flag);

            foreach (var pi in pis)
            {
                var attr = Attribute.GetCustomAttributes(pi, typeof(XmlAttributeAttribute))
                    .FirstOrDefault() as XmlAttributeAttribute;
                if (attr != null)
                {
                    if (string.Equals(
                        attr.AttributeName,
                        fieldName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        info = pi;
                        break;
                    }
                }
            }

            return info;
        }

        private static TimelineDefaultModel NewDefault(
            TimelineElementTypes element,
            string attr,
            object value)
            => new TimelineDefaultModel()
            {
                TargetElement = element,
                TargetAttribute = attr,
                Value = value.ToString(),
                Enabled = true,
            };
    }
}
