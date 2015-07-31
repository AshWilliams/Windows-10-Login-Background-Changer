﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;

namespace W10_BG_Logon_Changer.Tools
{
    public static class PriBuilder
    {
        public static void CreatePri(string currentPri, string outputPri, string image)
        {
            var inputStream = File.OpenRead(currentPri);
            var outputStream = File.Create(outputPri);
            var replacementStream = File.OpenRead(image);

            var inputReader = new BinaryReader(inputStream);
            var outputWriter = new BinaryWriter(outputStream);

            inputStream.CopyTo(outputStream);

            double f = replacementStream.Length/8;
            var replacementLengthAligned = (Math.Ceiling(f)*8);

            //Header
            inputStream.Seek(0x14, SeekOrigin.Begin);
            var headerLength = inputReader.ReadUInt32();
            inputStream.Seek(0xB8, SeekOrigin.Begin);
            var dataitemOffset = inputReader.ReadUInt32();
            var origDataitemLength = inputReader.ReadUInt32();
            var dataitemLength = origDataitemLength + replacementLengthAligned;
            outputStream.Seek(0xBC, SeekOrigin.Begin);
            outputWriter.Write((int) dataitemLength);

            //dataitem
            outputStream.Seek(headerLength + dataitemOffset + 0x18, SeekOrigin.Begin);
            outputWriter.Write((int) dataitemLength);
            inputStream.Seek(headerLength + dataitemOffset + 0x24, SeekOrigin.Begin);
            var stringCount = inputReader.ReadInt16();
            var blobCount = inputReader.ReadInt16();
            var origDataLength = inputReader.ReadInt32();
            outputStream.Seek(0xC, SeekOrigin.Current);
            outputWriter.Write((int) (origDataLength + replacementLengthAligned));
            outputStream.Seek(stringCount*4, SeekOrigin.Current);
            for (var i = 0; i < 10; i++)
            {
                outputWriter.Write(origDataLength);
                outputWriter.Write((int) replacementStream.Length);
            }
            outputStream.Seek((blobCount - 10)*8, SeekOrigin.Current);

            //data
            outputStream.Seek(origDataLength, SeekOrigin.Current);
            if (outputStream.Length - outputStream.Position != 0x18)
            {
                throw new Exception("Not compatible with this PRI file.");
            }
            replacementStream.CopyTo(outputStream);

            //footer
            outputStream.Seek((long) (replacementLengthAligned - replacementStream.Length), SeekOrigin.Current);
            outputWriter.Write(0xDEF5FADE);
            outputWriter.Write((int) dataitemLength);
            outputWriter.Write(0xDEFFFADE);
            outputWriter.Write(0x00000000);
            outputWriter.Write("mrm_pri2".ToCharArray());

            outputStream.Seek(0xC, SeekOrigin.Begin);
            outputWriter.Write((int) outputStream.Length);
            outputStream.Seek(-0xC, SeekOrigin.End);
            outputWriter.Write((int) outputStream.Length);

            inputReader.Close();
            outputWriter.Close();
            replacementStream.Close();
        }
    }
}