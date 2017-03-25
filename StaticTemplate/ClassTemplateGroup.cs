﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticTemplate
{
    public class ClassTemplateGroup : IEnumerable<ClassTemplate>
    {
        public IEnumerable<ClassTemplate> Templates { get; }
        public ClassTemplate PrimaryTemplate { get; }

        public int TypeParamCount { get { return PrimaryTemplate.TypeParamCount; } }
        public string TemplateName { get { return PrimaryTemplate.TemplateName; } }
        public int MaxParamCount { get { return PrimaryTemplate.TypeParamCount; } }

        public ClassTemplateGroup(IEnumerable<ClassTemplate> templates)
        {
            Templates = templates.ToList();  // we store a copy
            PrimaryTemplate = Templates.Where(t => !t.IsSpecialized).Single();
        }

        public IEnumerator<ClassTemplate> GetEnumerator() { return Templates.GetEnumerator(); }

        public string GetInstantiationNameFor(IEnumerable<TypeSyntax> typeArgs) =>
            $"{TemplateName}#{string.Join("_", typeArgs.ToString())}#";

        public ClassDeclarationSyntax Instantiate(IEnumerable<TypeSyntax> typeArgs) =>
            FindTemplateForArguments(typeArgs).Instantiate(GetInstantiationNameFor(typeArgs), typeArgs);

        public ClassTemplate FindTemplateForArguments(IEnumerable<TypeSyntax> typeArgs)
        {
            if (typeArgs.Count() != TypeParamCount)
                throw new InvalidOperationException("Incorrect number of type arguements for template" + TemplateName);

            var matched = Templates.Select(temp => Tuple.Create(temp, MatchTypeArgs(temp, typeArgs)))
                                                        .Where(t => t.Item2.Item1);
            var highestRanked = matched.MaxBy(t => t.Item2.Item2).Item2
                                       .Select(t => t.Item1);
            if (highestRanked.Count() == 1)
                return highestRanked.Single();

            // if there are more than one candidates, try determine which one is the most specialized (accepts fewest args)
            var hasFewestParams = highestRanked.MinBy(t => t.RemainingParamCount).Item2;
            if (hasFewestParams.Count() == 1)
                return hasFewestParams.Single();

            throw new Exception("Ambiguous partial specializations for " + GetInstantiationNameFor(typeArgs));
        }

        private Tuple<bool, int> MatchTypeArgs(ClassTemplate template, IEnumerable<TypeSyntax> typeArgs)
        {
            // TODO(leasunhy): use semantic model?
            var success = true;
            var matched = 0;
            template.SpecialiedTypeArgList.ZipWith(typeArgs, (a1, a2) =>
            {
                success = success && (a1 == null || a1.ToString() == a2.ToString());
                if (!success) return;
                matched += (a1 != null && a1.ToString() == a2.ToString()) ? 1 : 0;
            });
            return Tuple.Create(success, matched);
        }

        IEnumerator IEnumerable.GetEnumerator() { return Templates.GetEnumerator(); }
    }
}
