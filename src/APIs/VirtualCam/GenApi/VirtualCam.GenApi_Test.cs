using System;
using System.ComponentModel;

namespace GcLib;

public sealed partial class VirtualCam
{
    /// <summary>
    /// Nested class containing all publicly exposed GenApi device parameters.
    /// </summary>
    private sealed partial class GenApi : IDisposable
    {
        #region Backing fields

        private GcCommand _testCommand;
        private GcString _testString;
        private GcInteger _testInteger;
        private GcEnumeration _testEnumeration;
        private GcBoolean _testBoolean;
        private GcFloat _testFloat;

        private GcBoolean _acquisitionFailure;
        private GcEnumeration _acquisitionFailureEvent;
        private GcInteger _acquisitionTimeToFailure;

        #endregion

        // Provides properties for testing purposes (e.g. unit testing).

        #region Test

        /// <summary>
        /// Provides testing of a <see cref="GcCommand"/> parameter.
        /// </summary>
        [Category("Test")]
        public GcCommand TestCommand => _testCommand ??= new GcCommand(
            name: nameof(TestCommand),
            category: GetCategory(nameof(TestCommand)),
            description: "Parameter added to test command type camera properties.",
            method: () => { },
            visibility: GcVisibility.Guru);

        /// <summary>
        /// Provides testing of a <see cref="GcString"/> parameter.
        /// </summary>
        [Category("Test")]
        [DefaultValue("HelloWorld")]
        public GcString TestString => _testString ??= new GcString(
            name: nameof(TestString),
            category: GetCategory(nameof(TestString)),
            maxLength: 20,
            value: "HelloWorld",
            visibility: GcVisibility.Guru);

        /// <summary>
        /// Provides testing of a <see cref="GcInteger"/> parameter.
        /// </summary>
        [Category("Test")]
        [DefaultValue(42)]
        public GcInteger TestInteger => _testInteger ??= new GcInteger(
            name: nameof(TestInteger),
            category: GetCategory(nameof(TestInteger)),
            value: 42,
            min: 0,
            max: 100,
            increment: 1,
            incrementMode: EIncMode.fixedIncrement,
            visibility: GcVisibility.Guru);

        /// <summary>
        /// Provides testing of a <see cref="GcEnumeration"/> parameter.
        /// </summary>
        [Category("Test")]
        [DefaultValue(DayOfWeek.Friday)]
        public GcEnumeration TestEnumeration => _testEnumeration ??= new GcEnumeration(
            name: nameof(TestEnumeration),
            category: GetCategory(nameof(TestEnumeration)),
            DayOfWeek.Friday,
            typeof(DayOfWeek),
            visibility: GcVisibility.Guru);

        /// <summary>
        /// Provides testing of a <see cref="GcBoolean"/> parameter.
        /// </summary>
        [Category("Test")]
        [DefaultValue(false)]
        public GcBoolean TestBoolean => _testBoolean ??= new GcBoolean(
            name: nameof(TestBoolean),
            category: "Test",
            description: "Parameter added to test boolean type camera properties",
            value: false,
            isWritable: true,
            visibility: GcVisibility.Guru);

        /// <summary>
        /// Provides testing of a <see cref="GcFloat"/> parameter.
        /// </summary>
        [Category("Test")]
        [DefaultValue(3.1)]
        public GcFloat TestFloat => _testFloat ??= new GcFloat(
            name: nameof(TestFloat),
            category: GetCategory(nameof(TestFloat)),
            description: "Parameter added to test float type camera properties",
            value: 3.1,
            min: 0.0,
            max: 9.9,
            unit: "m",
            increment: 0.1,
            displayPrecision: 1,
            isWritable: true,
            visibility: GcVisibility.Guru);

        /// <summary>
        /// Provides a way to simulate a lost connection to a device.
        /// </summary>
        [Category("Test")]
        public GcCommand ForceDisconnect => new(name: "ForceDisconnect",
                                                category: "Test",
                                                () => { _virtualCam.OnConnectionLost(); },
                                                description: "Forces a disconnection of the device to simulate a lost connection.",
                                                visibility: GcVisibility.Guru);

        /// <summary>
        /// Provides a way to simulate hardware failures during acquisition.
        /// </summary>
        [Category("Test")]
        [DefaultValue(false)]
        public GcBoolean AcquisitionFailure => _acquisitionFailure ??= new GcBoolean(
            name: nameof(AcquisitionFailure),
            category: GetCategory(nameof(AcquisitionFailure)),
            description: $"Simulates a hardware failure during acquisition. The type of failure is defined in {nameof(AcquisitionFailureEvent)} and the time to failure is given by {nameof(AcquisitionTimeToFailure)}.",
            value: false,
            visibility: GcVisibility.Guru);

        /// <summary>
        /// Defines the type of hardware failure during acquisition to occur when <see cref="AcquisitionFailure"/> is set to true.
        /// </summary>
        [Category("Test")]
        [DefaultValue(AcquisitionFailureEventType.ConnectionLost)]
        public GcEnumeration AcquisitionFailureEvent => _acquisitionFailureEvent ??= new GcEnumeration(
            name: nameof(AcquisitionFailureEvent),
            category: GetCategory(nameof(AcquisitionFailureEvent)),
            description: "Event defining the type of failure to simulate.",
            enumEntry: AcquisitionFailureEventType.ConnectionLost,
            enumType: typeof(AcquisitionFailureEventType),
            visibility: GcVisibility.Guru);

        /// <summary>
        /// Defines the time delay to hardware failure from acquisition start when <see cref="AcquisitionFailure"/> is set to true.
        /// </summary>
        [Category("Test")]
        [DefaultValue(1000)]
        public GcInteger AcquisitionTimeToFailure => _acquisitionTimeToFailure ??= new GcInteger(
            name: nameof(AcquisitionTimeToFailure),
            category: GetCategory(nameof(AcquisitionTimeToFailure)),
            description: $"Time to hardware failure in milliseconds after a new acquisition has been started. To simulate hardware failure {nameof(AcquisitionFailure)} need to be set to true. The type of failure is defined by {nameof(AcquisitionFailureEvent)}.",
            value: 1000,
            min: 0,
            max: 5000,
            increment: 1,
            incrementMode: EIncMode.fixedIncrement,
            visibility: GcVisibility.Guru);

        #endregion
    }
}