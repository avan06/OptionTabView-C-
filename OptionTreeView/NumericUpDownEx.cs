using System;
using System.Windows.Forms;

namespace OptionTreeView
{
    /// <summary>
    /// Improvement of the Hexadecimal Property functionality in the NumericUpDown component
    /// 
    /// The limitation on the maximum value of the NumericUpDown component when the Hexadecimal Property is set has been addressed.
    /// The original behavior restricted the maximum value to Int32 when the Hexadecimal Property was set. It has now been corrected to allow a maximum value of Int64.
    /// https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.numericupdown.hexadecimal?view=netframework-4.8.1#remarks
    /// </summary>
    public class NumericUpDownEx : NumericUpDown
    {
        /// <summary>
        /// Returns the provided value constrained to be within the min and max.
        /// </summary>
        private decimal Constrain(decimal value)
        {
            if (value < Minimum) value = Minimum;
            if (value > Maximum) value = Maximum;

            return value;
        }

        /// <summary>
        ///  Converts the text displayed in the up-down control to a
        ///  numeric value and evaluates it.
        /// </summary>
        private new void ParseEditText()
        {
            try
            {
                // Verify that the user is not starting the string with a "-"
                // before attempting to set the Value property since a "-" is a valid character with
                // which to start a string representing a negative number.
                if (Hexadecimal && (!string.IsNullOrEmpty(base.Text) && (base.Text.Length != 1 || !(base.Text == "-"))))
                    Value = Constrain(Convert.ToDecimal(Convert.ToInt64(base.Text, 16)));
                else base.ParseEditText();
            }
            catch { /* Leave value as it is */ }
            finally { base.UserEdit = false; }
        }

        /// <summary>
        ///  Displays the current value of the up-down control in the appropriate format.
        /// </summary>
        protected override void UpdateEditText()
        {
            if (Hexadecimal)
            {
                // If the current value is user-edited, then parse this value before reformatting
                if (base.UserEdit) ParseEditText();
                if (!string.IsNullOrEmpty(Text))
                {
                    ChangingText = true;
                    Text = string.Format("{0:X}", (ulong)Math.Round(Value));
                }
            }
            else base.UpdateEditText();
        }

        /// <summary>
        ///  Validates and updates
        ///  the text displayed in the up-down control.
        /// </summary>
        protected override void ValidateEditText()
        {
            // See if the edit text parses to a valid decimal
            ParseEditText();
            UpdateEditText();
        }
    }
}
