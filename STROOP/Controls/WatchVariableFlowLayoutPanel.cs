﻿using STROOP.Forms;
using STROOP.Managers;
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
using System.Xml.Linq;

namespace STROOP.Controls
{
    public class WatchVariableFlowLayoutPanel : NoTearFlowLayoutPanel
    {
        private string _varFilePath;

        private readonly Object _objectLock;
        private List<WatchVariableControl> _watchVarControls;
        private readonly List<VariableGroup> _allGroups;
        private readonly List<VariableGroup> _initialVisibleGroups;
        private readonly List<VariableGroup> _visibleGroups;
        private List<ToolStripMenuItem> _filteringDropDownItems;

        private WatchVariableControl _reorderingWatchVarControl;

        public ContextMenuStrip SelectedContextMenuStrip;

        public WatchVariableFlowLayoutPanel()
        {
            _objectLock = new Object();
            _watchVarControls = new List<WatchVariableControl>();
            _allGroups = new List<VariableGroup>();
            _initialVisibleGroups = new List<VariableGroup>();
            _visibleGroups = new List<VariableGroup>();

            ContextMenuStrip = new ContextMenuStrip();

            _reorderingWatchVarControl = null;

            Click += (sender, e) => UnselectAllVariables();
        }

        public void Initialize(
            string varFilePath,
            List<VariableGroup> allGroups,
            List<VariableGroup> visibleGroups)
        {
            _varFilePath = varFilePath;
            _allGroups.AddRange(allGroups);
            _initialVisibleGroups.AddRange(visibleGroups);
            _visibleGroups.AddRange(visibleGroups);
            List<WatchVariableControlPrecursor> precursors = _varFilePath == null
                ? new List<WatchVariableControlPrecursor>()
                : XmlConfigParser.OpenWatchVariableControlPrecursors(_varFilePath);
            AddVariables(precursors.ConvertAll(precursor => precursor.CreateWatchVariableControl()));
            AddItemsToContextMenuStrip();
        }

        private void AddItemsToContextMenuStrip()
        {
            ToolStripMenuItem resetVariablesItem = new ToolStripMenuItem("Reset Variables");
            resetVariablesItem.Click += (sender, e) => ResetVariables();

            ToolStripMenuItem clearAllButHighlightedItem = new ToolStripMenuItem("Clear All But Highlighted");
            clearAllButHighlightedItem.Click += (sender, e) => ClearAllButHighlightedVariables();

            ToolStripMenuItem addAllToCustomTabItem = new ToolStripMenuItem("Add All to Custom Tab");
            addAllToCustomTabItem.Click += (sender, e) => AddAllVariablesToCustomTab();

            ToolStripMenuItem enableCustomizationItem = new ToolStripMenuItem("Enable Customization");
            enableCustomizationItem.Click += (sender, e) => EnableCustomVariableFunctionality();

            ToolStripMenuItem showVariableXmlItem = new ToolStripMenuItem("Show Variable XML");
            showVariableXmlItem.Click += (sender, e) => ShowVariableXml();

            ToolStripMenuItem showVariableInfoItem = new ToolStripMenuItem("Show Variable Info");
            showVariableInfoItem.Click += (sender, e) => ShowVariableInfo();

            ToolStripMenuItem openSaveClearItem = new ToolStripMenuItem("Open / Save / Clear ...");
            ControlUtilities.AddDropDownItems(
                openSaveClearItem,
                new List<string>() { "Open", "Save in Place", "Save As", "Clear" },
                new List<Action>()
                {
                    () => OpenVariables(),
                    () => SaveVariablesInPlace(),
                    () => SaveVariables(),
                    () => ClearVariables(),
                });

            ToolStripMenuItem setAllRoundingLimitsItem = new ToolStripMenuItem("Set all rounding limits...");
            ToolStripMenuItem setAllRoundingLimitsDefaultItem = new ToolStripMenuItem("Default");
            setAllRoundingLimitsDefaultItem.Click += (sender, e) =>
                ApplySettings(new WatchVariableControlSettings(
                    changeRoundingLimit: true, changeRoundingLimitToDefault: true));
            ToolStripMenuItem setAllRoundingLimitsNoRoundingItem = new ToolStripMenuItem("No Rounding");
            setAllRoundingLimitsNoRoundingItem.Click += (sender, e) =>
                ApplySettings(new WatchVariableControlSettings(
                    changeRoundingLimit: true, newRoundingLimit: -1));
            List<ToolStripMenuItem> setAllRoundingLimitsNumberItems = new List<ToolStripMenuItem>();
            for (int i = 0; i <= 10; i++)
            {
                int index = i;
                setAllRoundingLimitsNumberItems.Add(new ToolStripMenuItem(index + " decimal place(s)"));
                setAllRoundingLimitsNumberItems[index].Click += (sender, e) =>
                    ApplySettings(new WatchVariableControlSettings(
                        changeRoundingLimit: true, newRoundingLimit: index));
            }
            setAllRoundingLimitsItem.DropDownItems.Add(setAllRoundingLimitsDefaultItem);
            setAllRoundingLimitsItem.DropDownItems.Add(setAllRoundingLimitsNoRoundingItem);
            setAllRoundingLimitsNumberItems.ForEach(setAllRoundingLimitsNumberItem =>
            {
                setAllRoundingLimitsItem.DropDownItems.Add(setAllRoundingLimitsNumberItem);
            });

            ToolStripMenuItem setAllAngleSignedItem = new ToolStripMenuItem("Set all angle signed...");
            ToolStripMenuItem setAllAngleSignedDefaultItem = new ToolStripMenuItem("Default");
            setAllAngleSignedDefaultItem.Click += (sender, e) =>
                ApplySettings(new WatchVariableControlSettings(
                    changeAngleSigned: true, changeAngleSignedToDefault: true));
            ToolStripMenuItem setAllAngleSignedUnsignedItem = new ToolStripMenuItem("Unsigned");
            setAllAngleSignedUnsignedItem.Click += (sender, e) =>
                ApplySettings(new WatchVariableControlSettings(
                    changeAngleSigned: true, newAngleSigned: false));
            ToolStripMenuItem setAllAngleSignedSignedItem = new ToolStripMenuItem("Signed");
            setAllAngleSignedSignedItem.Click += (sender, e) =>
                ApplySettings(new WatchVariableControlSettings(
                    changeAngleSigned: true, newAngleSigned: true));
            setAllAngleSignedItem.DropDownItems.Add(setAllAngleSignedDefaultItem);
            setAllAngleSignedItem.DropDownItems.Add(setAllAngleSignedUnsignedItem);
            setAllAngleSignedItem.DropDownItems.Add(setAllAngleSignedSignedItem);

            ToolStripMenuItem setAllAngleUnitsItem = new ToolStripMenuItem("Set all angle units...");
            ToolStripMenuItem setAllAngleUnitsDefaultItem = new ToolStripMenuItem("Default");
            setAllAngleUnitsDefaultItem.Click += (sender, e) =>
                ApplySettings(new WatchVariableControlSettings(
                    changeAngleUnits: true, changeAngleUnitsToDefault: true));
            List<ToolStripMenuItem> setAllAngleUnitsValuesItems = new List<ToolStripMenuItem>();
            foreach (AngleUnitType angleUnitType in Enum.GetValues(typeof(AngleUnitType)))
            {
                AngleUnitType angleUnitTypeFixed = angleUnitType;
                ToolStripMenuItem setAllAngleUnitsValuesItem = new ToolStripMenuItem(angleUnitTypeFixed.ToString());
                setAllAngleUnitsValuesItem.Click += (sender, e) =>
                    ApplySettings(new WatchVariableControlSettings(
                        changeAngleUnits: true, newAngleUnits: angleUnitTypeFixed));
                setAllAngleUnitsValuesItems.Add(setAllAngleUnitsValuesItem);
            }
            setAllAngleUnitsItem.DropDownItems.Add(setAllAngleUnitsDefaultItem);
            setAllAngleUnitsValuesItems.ForEach(setAllAngleUnitsValuesItem =>
            {
                setAllAngleUnitsItem.DropDownItems.Add(setAllAngleUnitsValuesItem);
            });

            ToolStripMenuItem setAllAngleHexItem = new ToolStripMenuItem("Set all angle hex...");
            ToolStripMenuItem setAllAngleHexDefaultItem = new ToolStripMenuItem("Default");
            setAllAngleHexDefaultItem.Click += (sender, e) =>
                ApplySettings(new WatchVariableControlSettings(
                    changeAngleHex: true, changeAngleHexToDefault: true));
            ToolStripMenuItem setAllAngleHexHexItem = new ToolStripMenuItem("Hex");
            setAllAngleHexHexItem.Click += (sender, e) =>
                ApplySettings(new WatchVariableControlSettings(
                    changeAngleHex: true, newAngleHex: true));
            ToolStripMenuItem setAllAngleHexDecimalItem = new ToolStripMenuItem("Decimal");
            setAllAngleHexDecimalItem.Click += (sender, e) =>
                ApplySettings(new WatchVariableControlSettings(
                    changeAngleHex: true, newAngleHex: false));
            setAllAngleHexItem.DropDownItems.Add(setAllAngleHexDefaultItem);
            setAllAngleHexItem.DropDownItems.Add(setAllAngleHexHexItem);
            setAllAngleHexItem.DropDownItems.Add(setAllAngleHexDecimalItem);

            ToolStripMenuItem filterVariablesItem = new ToolStripMenuItem("Filter Variables...");
            _filteringDropDownItems = _allGroups.ConvertAll(varGroup => CreateFilterItem(varGroup));
            UpdateFilterItemCheckedStatuses();
            _filteringDropDownItems.ForEach(item => filterVariablesItem.DropDownItems.Add(item));
            filterVariablesItem.DropDown.AutoClose = false;
            filterVariablesItem.DropDown.MouseLeave += (sender, e) => { filterVariablesItem.DropDown.Close(); };

            ContextMenuStrip.Items.Add(resetVariablesItem);
            ContextMenuStrip.Items.Add(clearAllButHighlightedItem);
            ContextMenuStrip.Items.Add(addAllToCustomTabItem);
            ContextMenuStrip.Items.Add(enableCustomizationItem);
            ContextMenuStrip.Items.Add(showVariableXmlItem);
            ContextMenuStrip.Items.Add(showVariableInfoItem);
            ContextMenuStrip.Items.Add(openSaveClearItem);
            ContextMenuStrip.Items.Add(setAllRoundingLimitsItem);
            ContextMenuStrip.Items.Add(setAllAngleSignedItem);
            ContextMenuStrip.Items.Add(setAllAngleUnitsItem);
            ContextMenuStrip.Items.Add(setAllAngleHexItem);
            ContextMenuStrip.Items.Add(filterVariablesItem);
        }

        private void AddItemsToSelectedContextMenuStrip()
        {
            SelectedContextMenuStrip = new ContextMenuStrip();

            ToolStripMenuItem itemHighlight = new ToolStripMenuItem("Highlight...");
            ToolStripMenuItem itemLock = new ToolStripMenuItem("Lock...");
            ToolStripMenuItem itemCopy = new ToolStripMenuItem("Copy");
            ToolStripMenuItem itemPaste = new ToolStripMenuItem("Paste");

            ToolStripMenuItem itemRoundTo = new ToolStripMenuItem("Round to...");
            ToolStripMenuItem itemDisplayAsHex = new ToolStripMenuItem("Display as Hex...");

            ToolStripMenuItem itemAngleSigned = new ToolStripMenuItem("Angle: Signed...");
            ToolStripMenuItem itemAngleUnits = new ToolStripMenuItem("Angle: Units...");
            ToolStripMenuItem itemAngleTruncateToMultipleOf16 = new ToolStripMenuItem("Angle: Truncate to Multiple of 16...");
            ToolStripMenuItem itemConstrainToOneRevolution = new ToolStripMenuItem("Angle: Constrain to One Revolution...");

            ToolStripMenuItem itemDelete = new ToolStripMenuItem("Delete");
            ToolStripMenuItem itemOpenController = new ToolStripMenuItem("Open Controller");
            ToolStripMenuItem itemAddToCustomTab = new ToolStripMenuItem("Add to Custom Tab");

            SelectedContextMenuStrip.Items.Add(itemHighlight);
            SelectedContextMenuStrip.Items.Add(itemLock);
            SelectedContextMenuStrip.Items.Add(itemCopy);
            SelectedContextMenuStrip.Items.Add(itemPaste);
            SelectedContextMenuStrip.Items.Add(itemRoundTo);
            SelectedContextMenuStrip.Items.Add(itemDisplayAsHex);
            SelectedContextMenuStrip.Items.Add(itemAngleSigned);
            SelectedContextMenuStrip.Items.Add(itemAngleUnits);
            SelectedContextMenuStrip.Items.Add(itemAngleTruncateToMultipleOf16);
            SelectedContextMenuStrip.Items.Add(itemConstrainToOneRevolution);
            SelectedContextMenuStrip.Items.Add(itemDelete);
            SelectedContextMenuStrip.Items.Add(itemOpenController);
            SelectedContextMenuStrip.Items.Add(itemAddToCustomTab);
        }

        private ToolStripMenuItem CreateFilterItem(VariableGroup varGroup)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(varGroup.ToString());
            item.Click += (sender, e) => ToggleVarGroupVisibility(varGroup);
            return item;
        }

        private void ToggleVarGroupVisibility(VariableGroup varGroup, bool? newVisibilityNullable = null)
        {
            // Toggle visibility if no visibility is provided
            bool newVisibility = newVisibilityNullable ?? !_visibleGroups.Contains(varGroup);
            if (newVisibility) // change to visible
            {
                _visibleGroups.Add(varGroup);
            }
            else // change to hidden
            {
                _visibleGroups.Remove(varGroup);
            }
            UpdateControlsBasedOnFilters();
            UpdateFilterItemCheckedStatuses();
        }

        private void UpdateFilterItemCheckedStatuses()
        {
            if (_allGroups.Count != _filteringDropDownItems.Count) throw new ArgumentOutOfRangeException();

            for (int i = 0; i < _allGroups.Count; i++)
            {
                _filteringDropDownItems[i].Checked = _visibleGroups.Contains(_allGroups[i]);
            }
        }

        private void UpdateControlsBasedOnFilters()
        {
            lock (_objectLock)
            {
                Controls.Clear();
                _watchVarControls.ForEach(watchVarControl =>
                {
                    if (ShouldShow(watchVarControl))
                        Controls.Add(watchVarControl);
                });
            }
        }

        private void ApplySettings(WatchVariableControlSettings settings)
        {
            if (KeyboardUtilities.IsCtrlHeld())
            {
                WatchVariableControlSettingsManager.AddSettings(settings);
            }
            else
            {
                _watchVarControls.ForEach(control => control.ApplySettings(settings));
            }
        }

        public void AddVariable(WatchVariableControl watchVarControl)
        {
            lock (_objectLock)
            {
                AddVariables(new List<WatchVariableControl>() { watchVarControl });
            }
        }

        public void AddVariables(List<WatchVariableControl> watchVarControls)
        {
            lock (_objectLock)
            {
                foreach (WatchVariableControl watchVarControl in watchVarControls)
                {
                    _watchVarControls.Add(watchVarControl);
                    if (ShouldShow(watchVarControl)) Controls.Add(watchVarControl);
                    watchVarControl.SetPanel(this);
                }
            }
        }

        public void RemoveVariable(WatchVariableControl watchVarControl)
        {
            // No need to lock, since this calls into a method that locks
            RemoveVariables(new List<WatchVariableControl>() { watchVarControl });
        }

        public void RemoveVariables(List<WatchVariableControl> watchVarControls)
        {
            lock (_objectLock)
            {
                foreach (WatchVariableControl watchVarControl in watchVarControls)
                {
                    if (_reorderingWatchVarControl == watchVarControl)
                        _reorderingWatchVarControl = null;

                    _watchVarControls.Remove(watchVarControl);
                    if (ShouldShow(watchVarControl)) Controls.Remove(watchVarControl);
                    watchVarControl.SetPanel(null);
                }
            }
        }

        public void RemoveVariableGroup(VariableGroup varGroup)
        {
            List<WatchVariableControl> watchVarControls =
                _watchVarControls.FindAll(
                    watchVarControl => watchVarControl.BelongsToGroup(varGroup));
            RemoveVariables(watchVarControls);
        }

        public void ShowOnlyVariableGroup(VariableGroup visibleVarGroup)
        {
            ShowOnlyVariableGroups(new List<VariableGroup>() { visibleVarGroup });
        }

        public void ShowOnlyVariableGroups(List<VariableGroup> visibleVarGroups)
        {
            foreach (VariableGroup varGroup in _allGroups)
            {
                bool newVisibility = visibleVarGroups.Contains(varGroup);
                ToggleVarGroupVisibility(varGroup, newVisibility);
            }
        }

        public void ClearVariables()
        {
            List<WatchVariableControl> watchVarControlListCopy =
                new List<WatchVariableControl>(_watchVarControls);
            RemoveVariables(watchVarControlListCopy);
        }

        public void ClearAllButHighlightedVariables()
        {
            List<WatchVariableControl> nonHighlighted =
                _watchVarControls.FindAll(control => !control.Highlighted);
            RemoveVariables(nonHighlighted);
            _watchVarControls.ForEach(control => control.Highlighted = false);
        }

        private void ResetVariables()
        {
            ClearVariables();
            _visibleGroups.Clear();
            _visibleGroups.AddRange(_initialVisibleGroups);
            UpdateFilterItemCheckedStatuses();

            List<WatchVariableControlPrecursor> precursors = _varFilePath == null
                ? new List<WatchVariableControlPrecursor>()
                : XmlConfigParser.OpenWatchVariableControlPrecursors(_varFilePath);
            AddVariables(precursors.ConvertAll(precursor => precursor.CreateWatchVariableControl()));
        }

        private void UnselectAllVariables()
        {
            GetCurrentlySelectedVariableControls().ForEach(control => control.IsSelected = false);
        }

        private void AddAllVariablesToCustomTab()
        {
            GetCurrentVariableControls().ForEach(varControl =>
                varControl.AddToTab(Config.CustomManager));
        }

        private List<XElement> GetCurrentVarXmlElements(bool useCurrentState = true)
        {
            return GetCurrentVariableControls().ConvertAll(control => control.ToXml(useCurrentState));
        }

        private List<List<string>> GetCurrentVarInfo()
        {
            return GetCurrentVariableControls().ConvertAll(control => control.GetVarInfo());
        }

        public void ShowVariableXml()
        {
            InfoForm infoForm = new InfoForm();
            lock (_objectLock)
            {
                infoForm.SetText(
                    "Variable Info",
                    "Variable XML",
                    String.Join("\r\n", GetCurrentVarXmlElements()));
            }
            infoForm.Show();
        }

        public void ShowVariableInfo()
        {
            InfoForm infoForm = new InfoForm();
            lock (_objectLock)
            {
                infoForm.SetText(
                    "Variable Info",
                    "Variable Info",
                    String.Join("\t",
                        WatchVariableWrapper.GetVarInfoLabels()) +
                        "\r\n" +
                        String.Join("\r\n",
                            GetCurrentVarInfo().ConvertAll(
                                infoList => String.Join("\t", infoList))));
            }
            infoForm.Show();
        }

        public void OpenVariables()
        {
            List<XElement> elements = DialogUtilities.OpenXmlElements(FileType.StroopVariables);
            List<WatchVariableControlPrecursor> precursors =
                elements.ConvertAll(element => new WatchVariableControlPrecursor(element));
            AddVariables(precursors.ConvertAll(w => w.CreateWatchVariableControl()));
        }

        public void SaveVariablesInPlace()
        {
            if (!DialogUtilities.AskQuestionAboutSavingVariableFileInPlace()) return;
            SaveVariables(_varFilePath);
        }

        public void SaveVariables(string fileName = null)
        {
            DialogUtilities.SaveXmlElements(
                FileType.StroopVariables, "VarData", GetCurrentVarXmlElements(), fileName);
        }

        public void EnableCustomVariableFunctionality()
        {
            _watchVarControls.ForEach(control => control.EnableCustomFunctionality());
        }

        public void NotifyOfReordering(WatchVariableControl watchVarControl)
        {
            if (_reorderingWatchVarControl == null)
            {
                _reorderingWatchVarControl = watchVarControl;
                _reorderingWatchVarControl.FlashColor(WatchVariableControl.REORDER_START_COLOR);
            }
            else if (watchVarControl == _reorderingWatchVarControl)
            {
                _reorderingWatchVarControl.FlashColor(WatchVariableControl.REORDER_RESET_COLOR);
                _reorderingWatchVarControl = null;
            }
            else
            {
                int newIndex = Controls.IndexOf(watchVarControl);
                Controls.SetChildIndex(_reorderingWatchVarControl, newIndex);
                _reorderingWatchVarControl.FlashColor(WatchVariableControl.REORDER_END_COLOR);
                _reorderingWatchVarControl = null;
            }
        }

        public void NotifySelectClick(
            WatchVariableControl clickedControl, bool ctrlHeld, bool shiftHeld)
        {
            List<WatchVariableControl> currentControls = GetCurrentVariableControls();
            List<WatchVariableControl> currentlySelected =
                currentControls.FindAll(control => control.IsSelected);

            if (shiftHeld && currentlySelected.Count > 0)
            {
                int index1 = currentControls.IndexOf(currentlySelected.Last());
                int index2 = currentControls.IndexOf(clickedControl);
                int indexMin = Math.Min(index1, index2);
                int indexMax = Math.Max(index1, index2);
                for (int i = indexMin; i <= indexMax; i++)
                {
                    currentControls[i].IsSelected = true;
                }
            }
            else
            {
                bool toggle = ctrlHeld || (currentlySelected.Count == 1 && currentlySelected[0] == clickedControl);
                if (!toggle) currentControls.ForEach(control => control.IsSelected = false);
                clickedControl.IsSelected = !clickedControl.IsSelected;
            }
        }

        public List<WatchVariableControl> GetCurrentlySelectedVariableControls()
        {
            return GetCurrentVariableControls().FindAll(control => control.IsSelected);
        }

        public List<WatchVariableControl> GetCurrentVariableControls()
        {
            List<WatchVariableControl> watchVarControls = new List<WatchVariableControl>();
            lock (_objectLock)
            {
                foreach (Control control in Controls)
                {
                    WatchVariableControl watchVarControl = control as WatchVariableControl;
                    watchVarControls.Add(watchVarControl);
                }
            }
            return watchVarControls;
        }

        public List<WatchVariableControlPrecursor> GetCurrentVariablePrecursors()
        {
            return GetCurrentVariableControls().ConvertAll(control => control.WatchVarPrecursor);
        }

        public List<object> GetCurrentVariableValues(bool useRounding)
        {
            return GetCurrentVariableControls().ConvertAll(control => control.GetValue(useRounding));
        }

        public List<string> GetCurrentVariableNames()
        {
            return GetCurrentVariableControls().ConvertAll(control => control.VarName);
        }

        public void UpdatePanel()
        {
            if (!ContainsFocus)
            {
                UnselectAllVariables();
            }
            _watchVarControls.ForEach(watchVarControl => watchVarControl.UpdateControl());
        }

        private bool ShouldShow(WatchVariableControl watchVarControl)
        {
            return watchVarControl.BelongsToAnyGroupOrHasNoGroup(_visibleGroups);
        }

        public override string ToString()
        {
            List<string> varNames = _watchVarControls.ConvertAll(control => control.VarName);
            return String.Join(",", varNames);
        }
    }
}