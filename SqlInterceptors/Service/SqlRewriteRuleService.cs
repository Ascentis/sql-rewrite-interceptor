using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace Ascentis.Infrastructure
{
    public class SqlRewriteRuleService : IDisposable
    {
        private static bool _interceptorInited;
        private readonly ISqlRewriteRepository _repository;

        public delegate void ExceptionDelegate(Exception e);
        public ExceptionDelegate ExceptionDelegateEvent;

        public SqlRewriteRuleService(ISqlRewriteRepository repository, bool enabled = false)
        {
            _repository = repository;
            Enabled = enabled;
            SqlCommandInterceptor.ExceptionDelegateEvent += InvokeSqlCommandInterceptorExceptionDelegate;
        }

        private void InvokeSqlCommandInterceptorExceptionDelegate(Exception e)
        {
            ExceptionDelegateEvent?.Invoke(e);
        }

        public void RefreshRulesFromRepository()
        {
            lock (this)
            {
                var items = _repository.LoadSqlRewriteRules();
                SqlCommandRegExProcessor.SqlRewriteRules = items;
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
                    RefreshRulesFromRepository();

                if (!_interceptorInited)
                {
                    SqlInterceptorsInit.Init();
                    RegisterSqlCommandInjectors.Register();
                    _interceptorInited = true;
                }
                SqlCommandProcessorBase.Enabled = _enabled;
            }
        }

        private void ResetAutoRefreshTimerInterval()
        {
            _autoRefreshTimer?.Change(_autoRefreshRulesEnabled ? AutoRefreshTimerInterval : Timeout.Infinite,_autoRefreshRulesEnabled ? AutoRefreshTimerInterval : Timeout.Infinite);
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
        private bool _autoRefreshRulesEnabled;
        public bool AutoRefreshRulesEnabled
        {
            get => _autoRefreshRulesEnabled;
            set
            {
                if (value == _autoRefreshRulesEnabled)
                    return;
                // ReSharper disable once InconsistentlySynchronizedField
                if (value && !_repository.IsThreadSafe())
                    throw new SqlRewriteRuleServiceException("Repository must own it's connection (be thread safe) in order to use auto-refresh of Sql rewrite rules");
                _autoRefreshRulesEnabled = value;
                if (_autoRefreshTimer == null)
                    _autoRefreshTimer = new Timer(context =>
                    {
                        try
                        {
                            RefreshRulesFromRepository();
                        }
                        catch (Exception e)
                        {
                            InvokeSqlCommandInterceptorExceptionDelegate(e);
                            Enabled = false;
                        }
                    });
                ResetAutoRefreshTimerInterval();
            }
        }

        public int AddRule(string databaseRegEx, string queryMatchRegEx, string queryReplacementString, RegexOptions regExOptions = 0)
        {
            lock (this)
            {
                var rule = new SqlRewriteRule
                {
                    DatabaseRegEx = databaseRegEx,
                    QueryMatchRegEx = queryMatchRegEx,
                    QueryReplacementString = queryReplacementString,
                    RegExOptions = regExOptions
                };
                _repository.SaveSqlRewriteRule(rule);
                return rule.Id;
            }
        }

        public void RemoveRule(int id)
        {
            lock (this)
            {
                _repository.RemoveSqlRewriteRule(id);
            }
        }

        public void Dispose()
        {
            AutoRefreshRulesEnabled = false;
            _autoRefreshTimer?.Dispose();
            Enabled = false;
        }
    }
}
