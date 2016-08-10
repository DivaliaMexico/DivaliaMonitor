using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Configuration;
using DivaliaMonitor;

public class Program
{
    public static PersistentSettings settings = new PersistentSettings();

    [STAThread]
    public static void Main()
    {
        settings.Load(Path.ChangeExtension(Application.ExecutablePath, ".config"));
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        MyApplicationContext app = new MyApplicationContext(settings);
        Application.Run(app);
    }
}

class MyApplicationContext : ApplicationContext
{
    protected PersistentSettings settings;

    public MyApplicationContext(PersistentSettings settings)
    {
        NotifyIcon icon = new NotifyIcon();
        ContextMenu menu = new ContextMenu();
        MenuItem menuItem1 = new MenuItem();
        menu.MenuItems.AddRange(new MenuItem[] { menuItem1 });
        menuItem1.Index = 0;
        menuItem1.Text = "S&alir";
        menuItem1.Click += new EventHandler(delegate(Object o, EventArgs a) {
            Environment.Exit(0);
        });
        icon.Icon = new System.Drawing.Icon(@"Resources/smallicon.ico");
        icon.Visible = true;
        icon.ContextMenu = menu;

        this.settings = settings;
        new Thread(worker).Start();
    }

    void worker()
    {
        var Port = Convert.ToInt32(this.settings.GetValue("port", 5025));
        new HTTPServer.Server(Port);
    }
}