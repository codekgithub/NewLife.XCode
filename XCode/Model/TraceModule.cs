﻿using System;
using System.Text;
using NewLife;
using NewLife.Log;

namespace XCode;

/// <summary>链路追踪过滤器。自动给TraceId赋值</summary>
public class TraceModule : EntityModule
{
    #region 静态引用
    /// <summary>字段名</summary>
    public class __
    {
        /// <summary>链路追踪。用于APM性能追踪定位，还原该事件的调用链</summary>
        public static String TraceId = nameof(TraceId);
    }
    #endregion

    /// <summary>初始化。检查是否匹配</summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    protected override Boolean OnInit(Type entityType)
    {
        var fs = GetFields(entityType);
        foreach (var fi in fs)
        {
            if (fi.Type == typeof(String))
            {
                if (fi.Name.EqualIgnoreCase(__.TraceId)) return true;
            }
        }

        return false;
    }

    /// <summary>验证数据，自动加上创建和更新的信息</summary>
    /// <param name="entity"></param>
    /// <param name="isNew"></param>
    protected override Boolean OnValid(IEntity entity, Boolean isNew)
    {
        if (!isNew && !entity.HasDirty) return true;

        var traceId = DefaultSpan.Current?.TraceId;
        if (!traceId.IsNullOrEmpty())
        {
            var fs = GetFields(entity.GetType());

            // 多编码合并
            var old = entity[__.TraceId] as String;
            var ss = old?.Split(',').ToList();
            if (ss.Count > 0 && !ss.Contains(traceId))
            {
                ss.Add(traceId);

                // 最大长度
                var fs2 = fs.Where(e => e.Length > 0).ToList();
                var len = fs2.Count > 0 ? fs2.Min(e => e.Length) : 50;

                // 倒序取最后若干项
                var rs = "";
                for (var i = ss.Count - 1; i >= 0; i--)
                {
                    var str = ss.Skip(i).Join(",");
                    if (str.Length > len) break;

                    rs = str;
                }

                if (!rs.IsNullOrEmpty()) traceId = rs;
            }

            // 不管新建还是更新，都改变更新
            if (!traceId.IsNullOrEmpty()) SetNoDirtyItem(fs, entity, __.TraceId, traceId);
        }

        return true;
    }
}