/* Copyright (C) 2018 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using Utilities;

namespace DiskAccessLibrary.FileSystems.NTFS
{
    /// <remarks>
    /// Windows NT 3.51 sets the version to 1.0
    /// Windows NT 4.0 and later set the version to 1.1
    /// </remarks>
    public class LogRestartPage
    {
        private const string ValidSignature = "RSTR";
        private const int UpdateSequenceArrayOffset = 0x1E;

        /* Start of LFS_RESTART_PAGE_HEADER */
        // MULTI_SECTOR_HEADER
        public ulong ChkDskLsn;
        private uint SystemPageSize;
        private uint m_logPageSize;
        // ushort RestartOffset;
        public short MinorVersion;
        public short MajorVersion;
        public ushort UpdateSequenceNumber; // a.k.a. USN
        // byte[] UpdateSequenceReplacementData
        /* End of LFS_RESTART_PAGE_HEADER */
        public LogRestartArea LogRestartArea;

        public LogRestartPage()
        {
            LogRestartArea = new LogRestartArea();
        }

        public LogRestartPage(byte[] buffer, int offset)
        {
            MultiSectorHeader multiSectorHeader = new MultiSectorHeader(buffer, offset + 0x00);
            if (multiSectorHeader.Signature != ValidSignature)
            {
                throw new InvalidDataException("Invalid RSTR record signature");
            }
            ChkDskLsn = LittleEndianConverter.ToUInt64(buffer, offset + 0x08);
            SystemPageSize = LittleEndianConverter.ToUInt32(buffer, offset + 0x10);
            m_logPageSize = LittleEndianConverter.ToUInt32(buffer, offset + 0x14);
            ushort restartOffset = LittleEndianConverter.ToUInt16(buffer, offset + 0x18);
            MinorVersion = LittleEndianConverter.ToInt16(buffer, offset + 0x1A);
            MajorVersion = LittleEndianConverter.ToInt16(buffer, offset + 0x1C);
            int position = offset + multiSectorHeader.UpdateSequenceArrayOffset;
            List<byte[]> updateSequenceReplacementData = MultiSectorHelper.ReadUpdateSequenceArray(buffer, position, multiSectorHeader.UpdateSequenceArraySize, out UpdateSequenceNumber);
            MultiSectorHelper.DecodeSegmentBuffer(buffer, offset, UpdateSequenceNumber, updateSequenceReplacementData);
            LogRestartArea = new LogRestartArea(buffer, offset + restartOffset);
        }

        public byte[] GetBytes(int bytesPerLogPage)
        {
            m_logPageSize = (uint)bytesPerLogPage;
            LogRestartArea.LogPageDataOffset = (ushort)LogRecordPage.GetDataOffset(bytesPerLogPage);
            int strideCount = bytesPerLogPage / MultiSectorHelper.BytesPerStride;
            ushort updateSequenceArraySize = (ushort)(1 + strideCount);
            MultiSectorHeader multiSectorHeader = new MultiSectorHeader(ValidSignature, UpdateSequenceArrayOffset, updateSequenceArraySize);
            int restartOffset = (int)Math.Ceiling((double)(UpdateSequenceArrayOffset + updateSequenceArraySize * 2) / 8) * 8;
            
            byte[] buffer = new byte[bytesPerLogPage];
            multiSectorHeader.WriteBytes(buffer, 0);
            LittleEndianWriter.WriteUInt64(buffer, 0x08, ChkDskLsn);
            LittleEndianWriter.WriteUInt32(buffer, 0x10, SystemPageSize);
            LittleEndianWriter.WriteUInt32(buffer, 0x14, m_logPageSize);
            LittleEndianWriter.WriteUInt16(buffer, 0x18, (ushort)restartOffset);
            LittleEndianWriter.WriteInt16(buffer, 0x1A, MinorVersion);
            LittleEndianWriter.WriteInt16(buffer, 0x1C, MajorVersion);
            LogRestartArea.WriteBytes(buffer, restartOffset);

            // Write UpdateSequenceNumber and UpdateSequenceReplacementData
            List<byte[]> updateSequenceReplacementData = MultiSectorHelper.EncodeSegmentBuffer(buffer, 0, bytesPerLogPage, UpdateSequenceNumber);
            MultiSectorHelper.WriteUpdateSequenceArray(buffer, UpdateSequenceArrayOffset, updateSequenceArraySize, UpdateSequenceNumber, updateSequenceReplacementData);
            return buffer;
        }

        public static uint GetLogPageSize(byte[] buffer, int offset)
        {
            return LittleEndianConverter.ToUInt32(buffer, offset + 0x14);
        }

        public uint LogPageSize
        {
            get
            {
                return m_logPageSize;
            }
        }
    }
}
