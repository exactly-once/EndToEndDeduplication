using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Hosting.Helpers;
using NServiceBus.TransactionalSession.AcceptanceTests.Infrastructure;

namespace ExactlyOnce.AcceptanceTests.Infrastructure
{
    public static class EndpointCustomizationConfigurationExtensions
    {
        public static IScenarioWithEndpointBehavior<TContext> WithMachineInterfaceEndpoint<TContext>(this IScenarioWithEndpointBehavior<TContext> scenario, 
            EndpointConfigurationBuilder endpointConfigurationBuilder, Action<MachineInterfaceComponentBehaviorBuilder<TContext>> defineBehavior) 
            where TContext : ScenarioContext
        {
            var builder = new MachineInterfaceComponentBehaviorBuilder<TContext>(endpointConfigurationBuilder);
            defineBehavior(builder);

            return scenario.WithComponent(builder.Build());
        }

        public static IEnumerable<Type> GetTypesScopedByTestClass(this EndpointCustomizationConfiguration endpointConfiguration)
        {
            var assemblyScanner = new AssemblyScanner
            {
                ScanFileSystemAssemblies = false
            };

            var assemblies = assemblyScanner.GetScannableAssemblies();

            var assembliesToScan = assemblies.Assemblies
                //exclude acceptance tests by default
                .Where(a => a != Assembly.GetExecutingAssembly()).ToList();
            var types = assembliesToScan
                .SelectMany(a => a.GetTypes());

            types = types.Union(GetNestedTypeRecursive(endpointConfiguration.BuilderType.DeclaringType, endpointConfiguration.BuilderType));

            types = types.Union(endpointConfiguration.TypesToInclude);

            var typeList = types.Where(t => !endpointConfiguration.TypesToExclude.Contains(t)).ToList();

            typeList.Add(typeof(CaptureBuilderFeature));

            return typeList;
        }

        static IEnumerable<Type> GetNestedTypeRecursive(Type rootType, Type builderType)
        {
            if (rootType == null)
            {
                throw new InvalidOperationException("Make sure you nest the endpoint infrastructure inside the TestFixture as nested classes");
            }

            yield return rootType;

            if (typeof(IEndpointConfigurationFactory).IsAssignableFrom(rootType) && rootType != builderType)
            {
                yield break;
            }

            foreach (var nestedType in rootType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SelectMany(t => GetNestedTypeRecursive(t, builderType)))
            {
                yield return nestedType;
            }
        }
    }
}