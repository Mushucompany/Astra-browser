using System;
using System.IO;
using System.Windows.Forms;

class IELauncher
{
    [STAThread]
    static void Main(string[] args)
    {
        string url = args.Length > 0 ? args[0] : "about:blank";
        if (!string.IsNullOrEmpty(url) && !url.Contains("://") && !url.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
            url = "http://" + url;

        try
        {
            // Запуск IE как COM-объекта — без перенаправления на Edge
            Type ieType = Type.GetTypeFromProgID("InternetExplorer.Application");
            if (ieType == null)
            {
                MessageBox.Show("Компонент Internet Explorer не зарегистрирован в системе.",
                    "Internet Explorer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            dynamic ie = Activator.CreateInstance(ieType);
            ie.Visible = true;
            ie.Navigate(url);

            // Прячем окно регистрации->только показать браузер на переднем плане
            try { ie.Top = 20; ie.Left = 20; } catch { }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось запустить Internet Explorer:\n" + ex.Message,
                "АААААА", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}