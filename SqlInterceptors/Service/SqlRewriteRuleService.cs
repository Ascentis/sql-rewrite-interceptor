using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Ascentis.Infrastructure.SqlInterceptors.Injectors;
using Ascentis.Infrastructure.SqlInterceptors.Model;
using Ascentis.Infrastructure.SqlInterceptors.Plumbing;
using Ascentis.Infrastructure.SqlInterceptors.Repository;

namespace Ascentis.Infrastructure.SqlInterceptors
{
    public class SqlRewriteRuleService : IDisposable
    {
        private static bool _interceptorInited;
        private readonly ISqlRewriteRepository _repository;

        public delegate void ExceptionDelegate(Exception e);
        public delegate void AutoRefreshDelegate();
        public event ExceptionDelegate ExceptionEvent;
        public event AutoRefreshDelegate AutoRefreshEvent;

        public SqlRewriteRuleService(ISqlRewriteRepository repository, bool enabled = false)
        {
            _repository = repository;
            Enabled = enabled;
            SqlCommandInterceptor.ExceptionDelegateEvent += InvokeSqlCommandInterceptorExceptionDelegate;
        }

        private void InvokeSqlCommandInterceptorExceptionDelegate(Exception e)
        {
            ExceptionEvent?.Invoke(e);
        }

        public void RefreshRulesFromRepository()
        {
            IEnumerable<SqlRewriteRule> items;
            lock (this)
            {
                items = _repository.LoadSqlRewriteRules();
            }
            SqlCommandRegExProcessor.SqlRewriteRules = items;
        }

        public void ApplySettingsFromRepository()
        {
            IEnumerable<SqlRewriteSettings> settingsList;
            lock (this)
            {
                settingsList = _repository.LoadSqlRewriteSettings();
            }

            foreach (var settings in settingsList)
            {
                if (!settings.MatchMachineName() || !settings.MatchProcessName())
                    continue;
                Enabled = settings.Enabled;
                SqlCommandRegExProcessor.RegExInjectionEnabled = settings.RegExInjectionEnabled;
                SqlCommandTextStackTraceInjector.HashInjectionEnabled = settings.HashInjectionEnabled;
                SqlCommandTextStackTraceInjector.StackInjectionEnabled = settings.StackFrameInjectionEnabled;
                SqlCommandTextStackTraceInjector.StackFrameIgnorePrefixes = settings.StackFrameIgnorePrefixes;
                SqlCommandTextStackTraceInjector.CallStackEntriesToReport = settings.CallStackEntriesToReport;
                break;
            }
        }

        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                    return;
                _enabled = value;
                if (_enabled)
                {
                    ApplySettingsFromRepository();
                    RefreshRulesFromRepository();
                }

                if (!_interceptorInited)
                {
                    SqlInterceptorsInit.Init();
                    RegisterSqlCommandInjectors.Register();
                    _interceptorInited = true;
                }
                SqlCommandInterceptor.Enabled = _enabled;
            }
        }

        private void ResetAutoRefreshTimerInterval()
        {
            _autoRefreshTimer?.Change(_autoRefreshRulesAndSettingsEnabled ? AutoRefreshTimerInterval : Timeout.Infinite,
                _autoRefreshRulesAndSettingsEnabled ? AutoRefreshTimerInterval : Timeout.Infinite);
        }

        private int _autoRefreshTimerInterval = 60000;
        public int AutoRefreshTimerInterval
        {
            get => _autoRefreshTimerInterval;
            set
            {
                if (value == _autoRefreshTimerInterval)
                    return;
                _autoRefreshTimerInterval = value;
                ResetAutoRefreshTimerInterval();
            }
        }

        private Timer _autoRefreshTimer;
        private bool _autoRefreshRulesAndSettingsEnabled;
        public bool AutoRefreshRulesAndSettingsEnabled
        {
            get => _autoRefreshRulesAndSettingsEnabled;
            set
            {
                if (value == _autoRefreshRulesAndSettingsEnabled)
                    return;
                // ReSharper disable once InconsistentlySynchronizedField
                if (value && !_repository.IsThreadSafe())
                    throw new SqlRewriteRuleServiceException("Repository must own it's connection (be thread safe) in order to use auto-refresh of Sql rewrite rules and its settings");
                _autoRefreshRulesAndSettingsEnabled = value;
                _autoRefreshTimer ??= new Timer(context =>
                {
                    try
                    {
                        ApplySettingsFromRepository();
                        RefreshRulesFromRepository();
                        AutoRefreshEvent?.Invoke();
                    }
                    catch (Exception e)
                    {
                        InvokeSqlCommandInterceptorExceptionDelegate(e);
                    }
                });
                ResetAutoRefreshTimerInterval();
            }
        }

        public int AddRule(string databaseRegEx, string queryMatchRegEx, string queryReplacementString, RegexOptions regExOptions = 0)
        {
            var rule = new SqlRewriteRule
            {
                DatabaseRegEx = databaseRegEx,
                QueryMatchRegEx = queryMatchRegEx,
                QueryReplacementString = queryReplacementString,
                RegExOptions = regExOptions
            };
            lock (this)
            {
                _repository.SaveSqlRewriteRule(rule);
            }
            return rule.Id;
        }

        public void RemoveRule(int id)
        {
            lock (this)
            {
                _repository.RemoveSqlRewriteRule(id);
            }
        }

        public int StoreCurrentSettings(string machineRegEx, string processRegEx)
        {
            var settings = new SqlRewriteSettings
            {
                MachineRegEx = machineRegEx,
                ProcessNameRegEx = processRegEx,
                Enabled = Enabled,
                HashInjectionEnabled = SqlCommandTextStackTraceInjector.HashInjectionEnabled,
                StackFrameInjectionEnabled = SqlCommandTextStackTraceInjector.StackInjectionEnabled,
                RegExInjectionEnabled = SqlCommandRegExProcessor.RegExInjectionEnabled,
                StackFrameIgnorePrefixes = SqlCommandTextStackTraceInjector.StackFrameIgnorePrefixes,
                CallStackEntriesToReport = SqlCommandTextStackTraceInjector.CallStackEntriesToReport
            };
            lock (this)
            {
                _repository.SaveSqlRewriteSettings(settings);
            }
            return settings.Id;
        }

        public void RemoveSettings(int id)
        {
            lock (this)
            {
                _repository.RemoveSqlRewriteSettings(id);
            }
        }

        public void Dispose()
        {
            AutoRefreshRulesAndSettingsEnabled = false;
            _autoRefreshTimer?.Dispose();
            Enabled = false;
        }
    }
}
