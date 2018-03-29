﻿using STROOP.Controls;
using STROOP.Models;
using STROOP.Structs;
using STROOP.Structs.Configurations;
using STROOP.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace STROOP.Managers
{
    public class MemoryManager
    {
        private readonly BetterTextbox _textBoxMemoryStartAddress;
        private readonly Button _buttonMemoryButtonGo;
        private readonly CheckBox _checkBoxMemoryUpdateContinuously;
        private readonly CheckBox _checkBoxMemoryLittleEndian;
        private readonly ComboBox _comboBoxMemoryTypes;

        private readonly RichTextBoxEx _richTextBoxMemoryAddresses;
        private readonly RichTextBoxEx _richTextBoxMemoryBytes;
        private readonly RichTextBoxEx _richTextBoxMemoryValues;

        private readonly bool[] _objectDataBools;
        private bool[] _objectSpecificDataBools;

        public uint? Address { get; private set; }

        private BehaviorCriteria? _behavior;
        private BehaviorCriteria? Behavior
        {
            get
            {
                return _behavior;
            }
            set
            {
                if (value == _behavior) return;
                _behavior = value;
                if (_behavior.HasValue)
                {
                    List<WatchVariableControlPrecursor> precursors =
                        Config.ObjectAssociations.GetWatchVarControls(_behavior.Value)
                            .ConvertAll(control => control.WatchVarPrecursor);
                    _objectSpecificDataBools = ConvertPrecursorsToBoolArray(precursors);
                }
                else
                {
                    _objectSpecificDataBools = ConvertPrecursorsToBoolArray(null);
                }
            }
        }

        private static readonly int _memorySize = (int)ObjectConfig.StructSize;

        public MemoryManager(TabPage tabControl, List<WatchVariableControlPrecursor> objectData)
        {
            _textBoxMemoryStartAddress = tabControl.Controls["textBoxMemoryStartAddress"] as BetterTextbox;
            _buttonMemoryButtonGo = tabControl.Controls["buttonMemoryButtonGo"] as Button;
            _checkBoxMemoryUpdateContinuously = tabControl.Controls["checkBoxMemoryUpdateContinuously"] as CheckBox;
            _checkBoxMemoryLittleEndian = tabControl.Controls["checkBoxMemoryLittleEndian"] as CheckBox;
            _comboBoxMemoryTypes = tabControl.Controls["comboBoxMemoryTypes"] as ComboBox;

            _richTextBoxMemoryAddresses = tabControl.Controls["richTextBoxMemoryAddresses"] as RichTextBoxEx;
            _richTextBoxMemoryBytes = tabControl.Controls["richTextBoxMemoryBytes"] as RichTextBoxEx;
            _richTextBoxMemoryValues = tabControl.Controls["richTextBoxMemoryValues"] as RichTextBoxEx;

            _textBoxMemoryStartAddress.AddEnterAction(() => TryToSetAddressAndUpdateMemory());
            _buttonMemoryButtonGo.Click += (sender, e) => TryToSetAddressAndUpdateMemory();

            _comboBoxMemoryTypes.DataSource = TypeUtilities.SimpleTypeList;

            _objectDataBools = ConvertPrecursorsToBoolArray(objectData);

            Address = null;
            _behavior = null;
        }

        private bool[] ConvertPrecursorsToBoolArray(List<WatchVariableControlPrecursor> precursors)
        {
            bool[] boolArray = new bool[ObjectConfig.StructSize];
            if (precursors == null) return boolArray;
            foreach (WatchVariableControlPrecursor precursor in precursors)
            {
                WatchVariable watchVar = precursor.WatchVar;
                if (watchVar.BaseAddressType != BaseAddressTypeEnum.Object) continue;
                if (watchVar.IsSpecial) continue;
                if (watchVar.Mask != null) continue;

                uint offset = watchVar.Offset;
                int size = watchVar.ByteCount.Value;

                for (int i = 0; i < size; i++)
                {
                    boolArray[offset + i] = true;
                }
            }
            return boolArray;
        }

        private void TryToSetAddressAndUpdateMemory()
        {
            uint? addressNullable = ParsingUtilities.ParseHexNullable(_textBoxMemoryStartAddress.Text);
            if (addressNullable.HasValue) SetAddressAndUpdateMemory(addressNullable.Value);
        }

        public void SetAddressAndUpdateMemory(uint address)
        {
            _textBoxMemoryStartAddress.Text = HexUtilities.Format(address, 8);
            Address = address;
            UpdateMemory();
        }

        private class ValueText
        {
            public readonly int ByteIndex;
            public readonly int ByteSize;
            public readonly int StringIndex;
            public readonly int StringSize;
            private List<int> _byteIndexes;
            private List<int> _byteIndexesLittleEndian;
            
            public ValueText(int byteIndex, int byteSize, int stringIndex, int stringSize)
            {
                ByteIndex = byteIndex;
                ByteSize = byteSize;
                StringIndex = stringIndex;
                StringSize = stringSize;
                _byteIndexes = Enumerable.Range(byteIndex, byteSize).ToList();
                _byteIndexesLittleEndian = _byteIndexes.ConvertAll(
                    index => EndianUtilities.SwapEndianness(index));
            }

            public bool OverlapsData(bool[] dataBools, bool littleEndian)
            {
                List<int> byteIndexes = littleEndian ? _byteIndexesLittleEndian : _byteIndexes;
                return byteIndexes.Any(byteIndex => dataBools[byteIndex]);
            }
        }

        private void UpdateMemory()
        {
            if (!Address.HasValue) return;

            Behavior = new ObjectDataModel(Address.Value).BehaviorCriteria;

            byte[] bytes = Config.Stream.ReadRam(Address.Value, _memorySize);
            bool littleEndian = _checkBoxMemoryLittleEndian.Checked;
            Type type = TypeUtilities.StringToType[(string)_comboBoxMemoryTypes.SelectedItem];
            _richTextBoxMemoryAddresses.Text = FormatAddresses(Address.Value, _memorySize);
            _richTextBoxMemoryBytes.Text = FormatBytes(bytes, littleEndian);

            List<ValueText> valueTexts;
            _richTextBoxMemoryValues.Text = FormatValues(bytes, type, littleEndian, out valueTexts);
            valueTexts.ForEach(valueText =>
            {
                if (valueText.OverlapsData(_objectDataBools, littleEndian))
                {
                    _richTextBoxMemoryValues.SetBackColor(
                        valueText.StringIndex, valueText.StringSize, Color.LightPink);
                }
                else if (valueText.OverlapsData(_objectSpecificDataBools, littleEndian))
                {
                    _richTextBoxMemoryValues.SetBackColor(
                        valueText.StringIndex, valueText.StringSize, Color.LightGreen);
                }
            });
        }

        private string FormatAddresses(uint startAddress, int totalMemorySize)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < totalMemorySize; i += 16)
            {
                string whiteSpace = "\n";
                if (i == 0) whiteSpace = "";
                builder.Append(whiteSpace);

                uint address = startAddress + (uint)i;
                builder.Append(HexUtilities.Format(address, 8));
            }
            return builder.ToString();
        }

        private string FormatBytes(byte[] bytes, bool littleEndian)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                string whiteSpace = " ";
                if (i % 4 == 0) whiteSpace = "  ";
                if (i % 16 == 0) whiteSpace = "\n";
                if (i == 0) whiteSpace = "";
                builder.Append(whiteSpace);

                int byteIndex = i;
                if (littleEndian)
                {
                    int mod = i % 4;
                    int antiMod = 3 - mod;
                    byteIndex = byteIndex - mod + antiMod;
                }
                builder.Append(HexUtilities.Format(bytes[byteIndex], 2, false));
            }
            return builder.ToString();
        }

        private string FormatValues(byte[] bytes, Type type, bool littleEndian, out List<ValueText> valueTexts)
        {
            int typeSize = TypeUtilities.TypeSize[type];
            List<string> stringList = new List<string>();
            for (int i = 0; i < bytes.Length; i += typeSize)
            {
                string whiteSpace = " ";
                if (i % 4 == 0) whiteSpace = "  ";
                if (i % 16 == 0) whiteSpace = "\n";
                if (i == 0) whiteSpace = "";
                stringList.Add(whiteSpace);

                object value = TypeUtilities.ConvertBytes(type, bytes, i, littleEndian);
                stringList.Add(value.ToString());
            }

            List<int> indexList = Enumerable.Range(0, stringList.Count / 2).ToList()
                .ConvertAll(index => index * 2 + 1);
            int maxLength = indexList.Max(index => stringList[index].Length);
            indexList.ForEach(index =>
            {
                string oldString = stringList[index];
                string newString = oldString.PadLeft(maxLength, ' ');
                stringList[index] = newString;
            });

            valueTexts = new List<ValueText>();
            int totalLength = 0;
            for (int i = 0; i < stringList.Count; i++)
            {
                string stringValue = stringList[i];
                int stringLength = stringValue.Length;
                totalLength += stringLength;
                if (i % 2 == 1)
                {
                    int trimmedLength = stringValue.Trim().Length;
                    int valueIndex = (i - 1) / 2;
                    ValueText valueText = new ValueText(valueIndex * typeSize, typeSize, totalLength - trimmedLength, trimmedLength);
                    valueTexts.Add(valueText);
                }
            }

            StringBuilder builder = new StringBuilder();
            stringList.ForEach(stringValue => builder.Append(stringValue));
            return builder.ToString();
        }

        public void Update(bool updateView)
        {
            if (!updateView) return;

            if (_checkBoxMemoryUpdateContinuously.Checked) UpdateMemory();
        }
    }
}
