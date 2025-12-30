//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Lifeprojects.de">
//     Class: Program
//     Copyright © Lifeprojects.de 2025
// </copyright>
// <Template>
// 	Version 2.0.2025.0, 28.4.2025
// </Template>
//
// <author>Gerhard Ahrens - Lifeprojects.de</author>
// <email>developer@lifeprojects.de</email>
// <date>04.05.2025 19:34:00</date>
//
// <summary>
// Konsolen Applikation mit Menü
// </summary>
//-----------------------------------------------------------------------

namespace MediatorDemo
{
    /* Imports from NET Framework */
    using System;
    using System.Reflection;

    public class Program
    {
        private static void Main(string[] args)
        {
            ConsoleMenu.Add("1", "Mediator Demo", () => MenuPoint1());
            ConsoleMenu.Add("X", "Beenden", () => ApplicationExit());

            do
            {
                _ = ConsoleMenu.SelectKey(2, 2);
            }
            while (true);
        }

        private static void ApplicationExit()
        {
            Environment.Exit(0);
        }

        private static void MenuPoint1()
        {
            Console.Clear();

            var bus = new MessageBus();

            var a = new KlasseA(bus);
            var b = new KlasseB(bus);

            a.Start();

            // Referenz entfernen
            bus.Remove<NachrichtVonA>();

            a.Start(); // Klasse B reagiert NICHT mehr

            ConsoleMenu.Wait();
        }
    }

    public record NachrichtVonA(string Text, int Zahl);
    public record NachrichtVonB(bool Erfolg, DateTime Zeitstempel);

    public class WeakHandler
    {
        public WeakReference _Target { get; }
        public MethodInfo _Method { get; }

        public WeakHandler(Delegate handler)
        {
            this._Target = new WeakReference(handler.Target!);
            this._Method = handler.Method;
        }

        public bool Invoke(object message)
        {
            if (!_Target.IsAlive || _Target.Target == null)
            {
                return false;
            }

            this._Method.Invoke(this._Target.Target, new[] { message });
            return true;
        }
    }

    public class MessageBus
    {
        private readonly Dictionary<Type, List<WeakHandler>> _abonnenten = new();

        public void Abonnieren<T>(Action<T> handler)
        {
            Type type = typeof(T);

            if (this._abonnenten.ContainsKey(type) == false)
            {
                this._abonnenten[type] = new List<WeakHandler>();
            }

            this._abonnenten[type].Add(new WeakHandler(handler));
        }

        public void Senden<T>(T message)
        {
            Type type = typeof(T);

            if (this._abonnenten.ContainsKey(type) == false)
            {
                return;
            }

            var handlers = this._abonnenten[type];

            // Rückwärts iterieren → sicheres Entfernen
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                if (!handlers[i].Invoke(message!))
                {
                    handlers.RemoveAt(i); // GC-bereinigte Referenz
                }
            }
        }

        public void Remove<T>()
        {
            var type = typeof(T);

            if (this._abonnenten.Count > 0 && this._abonnenten.ContainsKey(type) == true)
            {
                this._abonnenten.Remove(type);
            }
        }
    }

    public class KlasseA
    {
        private readonly MessageBus _Bus;

        public KlasseA(MessageBus bus)
        {
            this._Bus = bus;
            this._Bus.Abonnieren<NachrichtVonB>(OnNachrichtVonB);
        }

        public void Start()
        {
            this._Bus.Senden(new NachrichtVonA("Hallo von A", 123));
        }

        private void OnNachrichtVonB(NachrichtVonB msg)
        {
            Console.WriteLine($"A empfängt: Erfolg={msg.Erfolg}, Zeit={msg.Zeitstempel}");
            Console.WriteLine("--------------------------");
        }
    }

    public class KlasseB
    {
        private readonly MessageBus _Bus;

        public KlasseB(MessageBus bus)
        {
            this._Bus = bus;
            this._Bus.Abonnieren<NachrichtVonA>(OnNachrichtVonA);
        }

        private void OnNachrichtVonA(NachrichtVonA msg)
        {
            Console.WriteLine($"B empfängt: Text='{msg.Text}', Zahl={msg.Zahl}");
            this._Bus.Senden(new NachrichtVonB(Erfolg: true, Zeitstempel: DateTime.Now));
        }
    }
}
