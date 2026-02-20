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
        public GcCommand TestCommand => _testCommand;

        /// <summary>
        /// Provides testing of a <see cref="GcString"/> parameter.
        /// </summary>
        [Category("Test")]
        [DefaultValue("HelloWorld")]
        public GcString TestString => _testString;

        /// <summary>
        /// Provides testing of a <see cref="GcInteger"/> parameter.
        /// </summary>
        [Category("Test")]
        [DefaultValue(42)]
        public GcInteger TestInteger => _testInteger;

        /// <summary>
        /// Provides testing of a <see cref="GcEnumeration"/> parameter.
        /// </summary>
        [Category("Test")]
        [DefaultValue(DayOfWeek.Friday)]
        public GcEnumeration TestEnumeration => _testEnumeration;

        /// <summary>
        /// Provides testing of a <see cref="GcBoolean"/> parameter.
        /// </summary>
        [Category("Test")]
        [DefaultValue(false)]
        public GcBoolean TestBoolean => _testBoolean;

        /// <summary>
        /// Provides testing of a <see cref="GcFloat"/> parameter.
        /// </summary>
        [Category("Test")]
        [DefaultValue(3.1)]
        public GcFloat TestFloat => _testFloat;

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
        public GcBoolean AcquisitionFailure => _acquisitionFailure;

        /// <summary>
        /// Defines the type of hardware failure during acquisition to occur when <see cref="AcquisitionFailure"/> is set to true.
        /// </summary>
        [Category("Test")]
        [DefaultValue(AcquisitionFailureEventType.ConnectionLost)]
        public GcEnumeration AcquisitionFailureEvent => _acquisitionFailureEvent;

        /// <summary>
        /// Defines the time delay to hardware failure from acquisition start when <see cref="AcquisitionFailure"/> is set to true.
        /// </summary>
        [Category("Test")]
        [DefaultValue(1000)]
        public GcInteger AcquisitionTimeToFailure => _acquisitionTimeToFailure;

        #endregion
    }
}