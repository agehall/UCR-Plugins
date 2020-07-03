using System;
using System.Reflection;
using HidWizards.UCR.Core.Attributes;
using HidWizards.UCR.Core.Models;
using HidWizards.UCR.Core.Models.Binding;
using HidWizards.UCR.Core.Utilities;
using HidWizards.UCR.Core.Utilities.AxisHelpers;

namespace AxisToAxisScaled
{
    [Plugin("Axis to Axis Scaled", Group = "Axis", Description = "Map from one axis to another and scale the output")]
    [PluginInput(DeviceBindingCategory.Range, "Axis")]
    [PluginOutput(DeviceBindingCategory.Range, "Axis")]
    [PluginSettingsGroup("Scaling", Group = "Scaling")]
    [PluginSettingsGroup("Sensitivity", Group = "Sensitivity")]
    [PluginSettingsGroup("Dead zone", Group = "Dead zone")]
    public class AdvAxisToAxis : Plugin
    {
        [PluginGui("Invert")]
        public bool Invert { get; set; }

        [PluginGui("Linear", Group = "Sensitivity", Order = 1)]
        public bool Linear { get; set; }

        [PluginGui("Percentage", Group = "Dead zone", Order = 0)]
        public int DeadZone { get; set; }

        [PluginGui("Anti-dead zone", Group = "Dead zone")]
        public int AntiDeadZone { get; set; }

        [PluginGui("Percentage", Group = "Sensitivity")]
        public int Sensitivity { get; set; }

        [PluginGui("Low range", Group = "Scaling")]
        public int LowRange { get; set; }

        [PluginGui("High range", Group = "Scaling")]
        public int HighRange { get; set; }

        private readonly DeadZoneHelper _deadZoneHelper = new DeadZoneHelper();
        private readonly AntiDeadZoneHelper _antiDeadZoneHelper = new AntiDeadZoneHelper();
        private readonly SensitivityHelper _sensitivityHelper = new SensitivityHelper();

        public AdvAxisToAxis()
        {
            DeadZone = 0;
            AntiDeadZone = 0;
            Sensitivity = 100;
            LowRange = 50;
            HighRange = 100;
        }

        public override void InitializeCacheValues()
        {
            Initialize();
        }

        public override void Update(params short[] values)
        {
            var value = values[0];


            // Empirical:
            // Throttle 0%   == 32k
            // Throttle 100% == -32k

            var axisPercentage = (32768 - value) / 65536f; // Percentage of input axis that is activated

            // rangeStart will represent where our range will begin
            int rangeStart = (int)Math.Ceiling((65536 * LowRange) / 100f);

            // rangeEnd will represent the end of our range
            int rangeEnd = (int)Math.Round((65536 * HighRange) / 100f);

            // outputRange is the range we are scaling the input axis to
            int outputRange = rangeEnd - rangeStart;

            // Calculate output value by scaling range to the absolute throttle percentage and offsetting with the max value and range start
            var tmp = 32768 - rangeStart - (int)Math.Round(outputRange * axisPercentage);

            // Debug.WriteLine($"d: {d} range: {range} Input: {value} output: {tmp}");

            // Let's limit the output value
            tmp = Math.Max(tmp, -32767);
            tmp = Math.Min(tmp, 32768);
            value = (short)tmp;

            if (Invert) value = Functions.Invert(value);
            if (DeadZone != 0) value = _deadZoneHelper.ApplyRangeDeadZone(value);
            if (AntiDeadZone != 0) value = _antiDeadZoneHelper.ApplyRangeAntiDeadZone(value);
            if (Sensitivity != 100) value = _sensitivityHelper.ApplyRangeSensitivity(value);
            WriteOutput(0, value);
        }

        private void Initialize()
        {
            _deadZoneHelper.Percentage = DeadZone;
            _antiDeadZoneHelper.Percentage = AntiDeadZone;
            _sensitivityHelper.Percentage = Sensitivity;
            _sensitivityHelper.IsLinear = Linear;
        }

        public override PropertyValidationResult Validate(PropertyInfo propertyInfo, dynamic value)
        {
            switch (propertyInfo.Name)
            {
                case nameof(DeadZone):
                case nameof(AntiDeadZone):
                case nameof(LowRange):
                case nameof(HighRange):
                    return InputValidation.ValidatePercentage(value);
            }

            return PropertyValidationResult.ValidResult;
        }
    }
}
