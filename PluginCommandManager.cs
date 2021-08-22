﻿using Dalamud.Game.Command;
using Dalamud.Plugin;
using EngageTimer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Dalamud.Game.Command.CommandInfo;

// ReSharper disable ForCanBeConvertedToForeach

namespace EngageTimer
{
    public class PluginCommandManager<THost> : IDisposable
    {
        private readonly DalamudPluginInterface _pluginInterface;
        private readonly CommandManager _commandManager;
        private readonly (string, CommandInfo)[] _pluginCommands;
        private readonly THost _host;

        public PluginCommandManager(THost host, DalamudPluginInterface pluginInterface, CommandManager commandManager)
        {
            this._pluginInterface = pluginInterface;
            this._commandManager = commandManager;
            this._host = host;

            this._pluginCommands = host.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public |
                                                             BindingFlags.Static | BindingFlags.Instance)
                .Where(method => method.GetCustomAttribute<CommandAttribute>() != null)
                .SelectMany(GetCommandInfoTuple)
                .ToArray();

            AddCommandHandlers();
        }

        private void AddCommandHandlers()
        {
            for (var i = 0; i < this._pluginCommands.Length; i++)
            {
                var (command, commandInfo) = this._pluginCommands[i];
                this._commandManager.AddHandler(command, commandInfo);
            }
        }

        private void RemoveCommandHandlers()
        {
            for (var i = 0; i < this._pluginCommands.Length; i++)
            {
                var (command, _) = this._pluginCommands[i];
                this._commandManager.RemoveHandler(command);
            }
        }

        private IEnumerable<(string, CommandInfo)> GetCommandInfoTuple(MethodInfo method)
        {
            var handlerDelegate = (HandlerDelegate)Delegate.CreateDelegate(typeof(HandlerDelegate), this._host, method);

            var command = handlerDelegate.Method.GetCustomAttribute<CommandAttribute>();
            var aliases = handlerDelegate.Method.GetCustomAttribute<AliasesAttribute>();
            var helpMessage = handlerDelegate.Method.GetCustomAttribute<HelpMessageAttribute>();
            var doNotShowInHelp = handlerDelegate.Method.GetCustomAttribute<DoNotShowInHelpAttribute>();

            var commandInfo = new CommandInfo(handlerDelegate)
            {
                HelpMessage = helpMessage?.HelpMessage ?? string.Empty,
                ShowInHelp = doNotShowInHelp == null,
            };

            // Create list of tuples that will be filled with one tuple per alias, in addition to the base command tuple.
            var commandInfoTuples = new List<(string, CommandInfo)> { (command.Command, commandInfo) };
            if (aliases == null) return commandInfoTuples;
            // ReSharper disable once LoopCanBeConvertedToQuery
            for (var i = 0; i < aliases.Aliases.Length; i++)
            {
                commandInfoTuples.Add((aliases.Aliases[i], commandInfo));
            }

            return commandInfoTuples;
        }

        public void Dispose()
        {
            RemoveCommandHandlers();
        }
    }
}