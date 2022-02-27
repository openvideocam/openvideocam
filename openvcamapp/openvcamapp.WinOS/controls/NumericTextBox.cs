using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace openvcamapp.winos.controls
{
    public class NumericTextBox : TextBox
    {
        static NumericTextBox()
        {
            EventManager.RegisterClassHandler(
                typeof(NumericTextBox),
                DataObject.PastingEvent,
                (DataObjectPastingEventHandler)((sender, e) =>
                {
                    if (!IsDataValid(e.DataObject))
                    {
                        DataObject data = new DataObject();
                        data.SetText(String.Empty);
                        e.DataObject = data;
                        e.Handled = false;
                    }
                }));
        }

        protected override void OnDrop(DragEventArgs e)
        {
            e.Handled = !IsDataValid(e.Data);
            base.OnDrop(e);
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            if (!IsDataValid(e.Data))
            {
                e.Handled = true;
                e.Effects = DragDropEffects.None;
            }

            base.OnDragEnter(e);
        }

        private static Boolean IsDataValid(IDataObject data)
        {
            Boolean isValid = false;
            if (data != null)
            {
                String text = data.GetData(DataFormats.Text) as String;
                if (!String.IsNullOrEmpty(text == null ? null : text.Trim()))
                {
                    Int32 result = -1;
                    if (Int32.TryParse(text, out result))
                    {
                        if (result > 0)
                        {
                            isValid = true;
                        }
                    }
                }
            }

            return isValid;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key < Key.D0 || e.Key > Key.D9)
            {
                if (e.Key < Key.NumPad0 || e.Key > Key.NumPad9)
                {
                    if (e.Key != Key.Back)
                    {
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
