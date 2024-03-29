﻿using DryIoc;
using System;
using System.Collections.Generic;
using System.Data;

namespace ServerTools.ServerCommands
{
    public class CommandContainer
    {
        Container c;
        Dictionary<Type, Type> commandresponsetypemap;
        public CommandContainer()
        {
            c = new Container();
            commandresponsetypemap = new Dictionary<Type, Type>();
        }


        #region Commands

        public CommandContainer RegisterCommand<T>(bool IsSingleton = true, bool ResolveConstructorArguments = false) where T : IRemoteCommand
        {
            c.Register<T>(reuse: Reuse.Singleton);

            c.Register<IRemoteCommand, T>(reuse: IsSingleton ? Reuse.Singleton : Reuse.Transient, serviceKey: typeof(T).Name, made: (ResolveConstructorArguments ? FactoryMethod.ConstructorWithResolvableArguments : null));

            return this;
        }
        public CommandContainer RegisterCommand<T>(T instance) where T : IRemoteCommand
        {
            c.RegisterInstance<IRemoteCommand>(instance, serviceKey: typeof(T).Name);

            return this;
        }
        public CommandContainer RegisterCommand<T>(Type[] constructor_types, bool IsSingleton = true) where T : IRemoteCommand
        {
            c.Register<IRemoteCommand>(reuse: IsSingleton ? Reuse.Singleton : Reuse.Transient, made: Made.Of(typeof(T).GetConstructor(constructor_types)), serviceKey: typeof(T).Name);

            return this;
        }

        public CommandContainer RegisterCommand(Type command_type) 
        {
            c.Register<IRemoteCommand>(serviceKey: command_type.Name);

            return this;
        }

        public bool IsCommandRegistered<T>() where T : IRemoteCommand
        {
            return c.IsRegistered<IRemoteCommand>(serviceKey: typeof(T).Name);
        }

        public bool IsCommandRegistered(string ServiceKey)
        {
            return c.IsRegistered<IRemoteCommand>(serviceKey: ServiceKey);
        }

        public bool IsCommandRegistered(Type type)
        {
            return c.IsRegistered<IRemoteCommand>(serviceKey: type.Name);
        }

        public IRemoteCommand ResolveCommand<T>() where T : IRemoteCommand
        {
            return c.Resolve<IRemoteCommand>(serviceKey: typeof(T).Name);
        }

        public IRemoteCommand ResolveCommand(string serviceKey)
        {
            return c.Resolve<IRemoteCommand>(serviceKey: serviceKey);
        }

        public CommandContainer RegisterCommand<TCommand, TResponse>(bool IsSingleton = true, bool ResolveConstructorArguments = false) where TCommand : IRemoteCommand where TResponse : IRemoteResponse
        {
            return RegisterResponse<TCommand, TResponse>(IsSingleton, ResolveConstructorArguments);
        }

        #endregion

            #region Responses

        public CommandContainer RegisterResponse<TCommand, TResponse>(bool IsSingleton = true, bool ResolveConstructorArguments = false) where TCommand: IRemoteCommand where TResponse : IRemoteResponse
        {
            if (!IsCommandRegistered<TCommand>())
            {
                RegisterCommand<TCommand>();
            }

            c.Register<TResponse>(reuse: Reuse.Singleton);

            c.Register<IRemoteResponse, TResponse>(reuse: IsSingleton ? Reuse.Singleton : Reuse.Transient, serviceKey: typeof(TResponse).Name, made: (ResolveConstructorArguments ? FactoryMethod.ConstructorWithResolvableArguments : null));
            
            commandresponsetypemap.Add(typeof(TCommand), typeof(TResponse));

            return this;
        }
        public CommandContainer RegisterResponse<TCommand, TResponse>(TResponse instance) where TCommand : IRemoteCommand where TResponse : IRemoteResponse
        {
            if (!IsCommandRegistered<TCommand>())
            {
                RegisterCommand<TCommand>();
            }

            c.RegisterInstance<IRemoteResponse>(instance, serviceKey: typeof(TResponse).Name);
            
            commandresponsetypemap.Add(typeof(TCommand), typeof(TResponse));
            
            return this;
        }
        public CommandContainer RegisterResponse<TCommand, TResponse>(Type[] const_types, bool IsSingleton = true) where TCommand : IRemoteCommand where TResponse : IRemoteResponse
        {
            if (!IsCommandRegistered<TCommand>())
            {
                RegisterCommand<TCommand>();
            }

            c.Register<IRemoteResponse>(reuse: IsSingleton ? Reuse.Singleton : Reuse.Transient, made: Made.Of(typeof(TResponse).GetConstructor(const_types)), serviceKey: typeof(TResponse).Name);
            
            commandresponsetypemap.Add(typeof(TCommand), typeof(TResponse));

            return this;
        }
        public CommandContainer RegisterResponse(Type response_type)
        {
            c.Register<IRemoteResponse>(serviceKey: response_type.Name);

            return this;
        }
        public bool IsResponseRegistered<TCommand, TResponse>() where TCommand : IRemoteCommand where TResponse : IRemoteResponse
        {
            return c.IsRegistered<IRemoteResponse>(serviceKey: typeof(TResponse).Name) && commandresponsetypemap.ContainsKey(typeof(TCommand));
        }

        public bool IsResponseRegistered(string ServiceKey)
        {
            return c.IsRegistered<IRemoteResponse>(serviceKey: ServiceKey) && commandresponsetypemap.ContainsValue(ResolveResponse(ServiceKey).GetType());  
        }
        public bool IsResponseRegistered(Type type)
        {
            return c.IsRegistered<IRemoteResponse>(serviceKey: type.Name);
        }
        public bool IsResponseRegisteredForCommand(Type commandtype)
        {
            return commandresponsetypemap.ContainsKey(commandtype) ? IsResponseRegistered(commandresponsetypemap[commandtype]) : false;
        }

        public IRemoteResponse ResolveResponse<TResponse>() where TResponse : IRemoteResponse
        {
            return c.Resolve<IRemoteResponse>(serviceKey: typeof(TResponse).Name);
        }

        public IRemoteResponse ResolveResponse(string serviceKey)
        {
            return c.Resolve<IRemoteResponse>(serviceKey: serviceKey);
        }
        public IRemoteResponse ResolveResponseFromCommand(Type CommandType)
        {
            var com = ResolveCommand(CommandType.Name);
            var respType = commandresponsetypemap[com.GetType()];
            return c.Resolve<IRemoteResponse>(serviceKey: respType.Name);
        }

        #endregion


        public CommandContainer RegisterDependency<G, T>(bool IsSingleton = true, bool ResolveConstructorArguments = false, string ServiceKey = null)
        {
            c.Register(typeof(G), typeof(T), reuse: IsSingleton ? Reuse.Singleton : Reuse.Transient, serviceKey: ServiceKey, made: (ResolveConstructorArguments ? FactoryMethod.ConstructorWithResolvableArguments : null)); ;

            return this;
        }
        public CommandContainer Use<T>(T instance)
        {
            c.Use(instance);

            return this;
        }

        
    }
}
