﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase.Config;
using System.Reflection;
using SuperSocket.Common;
using SuperSocket.SocketBase.Command;

namespace SuperWebSocket.SubProtocol
{
    public class BasicSubProtocol : BasicSubProtocol<WebSocketSession>
    {
        public BasicSubProtocol(string name, IEnumerable<Assembly> commandAssemblies, ICommandParser commandParser)
            : base(name, commandAssemblies, commandParser)
        {

        }

        public BasicSubProtocol(string name, IEnumerable<Assembly> commandAssemblies)
            : base(name, commandAssemblies)
        {

        }

        public BasicSubProtocol()
            : base()
        {

        }
    }

    public class BasicSubProtocol<TWebSocketSession> : ISubProtocol<TWebSocketSession>
        where TWebSocketSession : WebSocketSession<TWebSocketSession>, new()
    {
        public const string DefaultName = "Basic";

        private List<Assembly> m_CommandAssemblies = new List<Assembly>();

        private Dictionary<string, ISubCommand<TWebSocketSession>> m_CommandDict;

        public BasicSubProtocol(IEnumerable<Assembly> commandAssemblies)
            : this(DefaultName, commandAssemblies, new BasicSubCommandParser())
        {

        }

        public BasicSubProtocol()
            : this(DefaultName, new List<Assembly> { Assembly.GetEntryAssembly() }, new BasicSubCommandParser())
        {

        }

        public BasicSubProtocol(string name, IEnumerable<Assembly> commandAssemblies)
            : this(name, commandAssemblies, new BasicSubCommandParser())
        {

        }

        public BasicSubProtocol(string name, IEnumerable<Assembly> commandAssemblies, ICommandParser commandParser)
        {
            Name = name;
            //The items in commandAssemblies may be null, so filter here
            m_CommandAssemblies.AddRange(commandAssemblies.Where(a => a != null));
            SubCommandParser = commandParser;
        }

        #region ISubProtocol Members

        public ICommandParser SubCommandParser { get; private set; }

        private void DiscoverCommands()
        {
            var subCommands = new List<ISubCommand<TWebSocketSession>>();

            foreach (var assembly in m_CommandAssemblies)
            {
                subCommands.AddRange(assembly.GetImplementedObjectsByInterface<ISubCommand<TWebSocketSession>>());
            }

            m_CommandDict = new Dictionary<string, ISubCommand<TWebSocketSession>>(subCommands.Count, StringComparer.OrdinalIgnoreCase);
            subCommands.ForEach(c => m_CommandDict.Add(c.Name, c));
        }

        public bool Initialize(IServerConfig config)
        {
            var commandAssembly = config.Options.GetValue("commandAssembly");

            if (string.IsNullOrEmpty(commandAssembly))
                return true;

            var protocolAssemblies = commandAssembly.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < protocolAssemblies.Length; i++)
            {
                var p = protocolAssemblies[i].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                if (p.Length == 1)
                {
                    if (Name.Equals(DefaultName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!ResolveCommmandAssembly(p[0]))
                            return false;

                        continue;
                    }
                }
                else if (p.Length == 2)
                {
                    if (string.IsNullOrEmpty(p[0]) || string.IsNullOrEmpty(p[1]))
                        continue;

                    if (Name.Equals(p[0].Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        if (!ResolveCommmandAssembly(p[1]))
                            return false;

                        continue;
                    }
                }
                else
                {
                    LogUtil.LogError("Invalid command assembly: " + commandAssembly);
                    return false;
                }
            }

            DiscoverCommands();

            return true;
        }

        private bool ResolveCommmandAssembly(string definition)
        {
            try
            {
                var assemblies = AssemblyUtil.GetAssembliesFromString(definition);

                if (assemblies.Any())
                    m_CommandAssemblies.AddRange(assemblies);

                return true;
            }
            catch (Exception e)
            {
                LogUtil.LogError(e);
                return false;
            }
        }

        public bool TryGetCommand(string name, out ISubCommand<TWebSocketSession> command)
        {
            return m_CommandDict.TryGetValue(name, out command);
        }

        public string Name { get; private set; }

        #endregion
    }
}
