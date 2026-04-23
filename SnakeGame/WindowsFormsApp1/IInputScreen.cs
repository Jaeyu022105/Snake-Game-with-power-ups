using System.Windows.Forms;

namespace WindowsFormsApp1
{
    internal interface IInputScreen
    {
        bool HandleKeyInput(Keys keyData);
    }
}
