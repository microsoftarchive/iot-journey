// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Practices.IoTJourney.ColdStorage.EventProcessor.RollingBlobWriter
{
    public class BlockData
    {
        private byte[] _frame = null;

        private int _actualFrameLength = 0;

        public BlockData(byte[] frame, int frameLength)
        {
            Guard.ArgumentNotNull(frame, "frame");
            Guard.ArgumentGreaterOrEqualThan(0, frameLength, "frameLength");
            Guard.ArgumentLowerOrEqualThan(frame.Length, frameLength, "frameLength");

            _frame = frame;
            _actualFrameLength = frameLength;
        }

        public byte[] Frame
        {
            get { return _frame; }
        }

        public int FrameLength
        {
            get { return _actualFrameLength; }
        }
    }
}