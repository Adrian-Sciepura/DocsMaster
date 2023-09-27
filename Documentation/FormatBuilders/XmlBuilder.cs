using Documentation.Models;
using Documentation.Models.CodeElements;
using Documentation.Models.CodeElements.Methods;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Types;
using Documentation.Models.CodeElements.Variables;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Documentation.FormatBuilders
{
    internal class XmlBuilder : FormatBuilder
    {
        private static string _outputFolder;

        private void CopyXmlSchema()
        {
            using (Stream style = Assembly.GetExecutingAssembly().GetManifestResourceStream("Documentation.Resources.schema.xsd"))
            {
                using (Stream output = File.OpenWrite(Path.Combine(_outputFolder, "schema.xsd")))
                    style.CopyTo(output);
            }
        }

        private static void RunForEveryNamespace(ProjectStructureTreeNode node, XElement parentElement)
        {
            XElement temp = parentElement;
            if (node.NamespaceReference != null)
            {
                temp = ConvertToXml(node.NamespaceReference);
                parentElement.Add(temp);
            }


            foreach (var child in node.Childs)
                RunForEveryNamespace(child, temp);
        }

        public XmlBuilder(DocsInfo docsInfo, ProjectStructureTree projectStructureTree) :
            base(docsInfo, projectStructureTree)
        {
            _outputFolder = Path.Combine(docsInfo.DocsPath, "xml");

            if (!Directory.Exists(_outputFolder))
                Directory.CreateDirectory(_outputFolder);
        }

        public override async Task Generate()
        {
            XElement root = new XElement("documentation");
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

            root.Add(new XAttribute(XNamespace.Xmlns + "xsi", xsi));
            root.Add(new XAttribute(xsi + "noNamespaceSchemaLocation", "schema.xsd"));

            RunForEveryNamespace(_solutionTree.root, root);

            XDocument docsFile = new XDocument(root);
            docsFile.Save(Path.Combine(_outputFolder, "docs.xml"));

            CopyXmlSchema();
        }



        #region Converters Invoke Management

        private delegate XElement ActionForCodeElement(CodeElement codeElement);

        private static Dictionary<CodeElementType, ActionForCodeElement> XmlConverters = new Dictionary<CodeElementType, ActionForCodeElement>()
        {
            { CodeElementType.Namespace, NamespaceConvert },
            { CodeElementType.Property, PropertyConvert },
            { CodeElementType.Variable, VariableConvert },
            { CodeElementType.Constructor, ConstructorConvert },
            { CodeElementType.Destructor, DestructorConvert },
            { CodeElementType.Method, MethodConvert },
            { CodeElementType.Operator, OperatorConvert },
            { CodeElementType.Delegate, DelegateConvert },
            { CodeElementType.Interface, TypeConvert },
            { CodeElementType.Class, TypeConvert },
            { CodeElementType.Struct, TypeConvert },
            { CodeElementType.Record, TypeConvert },
            { CodeElementType.Enum, EnumConvert },
        };

        private static XElement ConvertToXml(CodeElement codeElement)
        {
            return XmlConverters[codeElement.Type](codeElement);
        }

        #endregion



        #region Converters


        #region Help Functions

        private static XElement DeclarationConvert(BaseCodeDeclarationKind declaration, string declarationType)
        {
            StringBuilder fullName = new StringBuilder();

            XElement ConvertSingleElement(BaseCodeDeclarationKind element)
            {
                XElement result;

                switch (element)
                {
                    case CodeRegularDeclaration regularDeclaration:
                        result = new XElement("regularDeclaration", regularDeclaration.Name);

                        if(regularDeclaration.FullName != null)
                            result.Add(new XAttribute("fullName", regularDeclaration.FullName));
                        
                        fullName.Append(regularDeclaration.Name);

                        break;
                    case CodeGenericDeclaration genericDeclaration:
                        result = new XElement("genericDeclaration", genericDeclaration.MainType.Name);

                        if(genericDeclaration.MainType.FullName != null)
                            result.Add(new XAttribute("fullName", genericDeclaration.MainType.FullName));


                        result.Add(new XAttribute("nrOfTypeParams", genericDeclaration.SubTypes.Count));
                        fullName.Append(genericDeclaration.MainType.Name);


                        fullName.Append('<');
                        for (int i = 0; i < genericDeclaration.SubTypes.Count; i++)
                        {
                            result.Add(ConvertSingleElement(genericDeclaration.SubTypes[i]));

                            if(i != genericDeclaration.SubTypes.Count - 1)
                                fullName.Append(", ");
                        }

                        fullName.Append('>');
                        break;
                    default:
                        throw new ArgumentException("Unknown Declaration Kind");
                }

                return result;
            }

            
            return new XElement("declaration", ConvertSingleElement(declaration), new XAttribute("type", declarationType), new XAttribute("name", fullName.ToString()));
        }

        private static XElement? ModifiersConvert(CodeElement codeElement)
        {
            if(codeElement.AccessModifier == null || codeElement.AccessModifier.Trim() == string.Empty)
                return null;

            XElement modifiers = new XElement("modifiers");

            string[] modifierList = codeElement.AccessModifier.Split(' ');

            foreach (string modifier in modifierList)
                modifiers.Add(new XElement("modifier", modifier));

            return modifiers;
        }
        
        private static XElement? MathodParamsConvert(List<CodeField> methodParameters)
        {
            if (methodParameters.Count == 0)
                return null;

            XElement parameters = new XElement("parameters");

            foreach(var parameter in methodParameters)
                parameters.Add(ConvertToXml(parameter));

            return parameters;
        }
        
        #endregion

        private static XElement NamespaceConvert(CodeElement codeElement)
        {
            CodeNamespace codeNamespace = codeElement as CodeNamespace;
            XElement namespaceElement = new XElement("namespace");

            namespaceElement.Add(DeclarationConvert(codeNamespace.Declaration, "name"));
            
            if(codeNamespace.InternalTypes.Count > 0)
            {
                XElement members = new XElement("members");

                foreach (var member in codeNamespace.InternalTypes)
                    members.Add(ConvertToXml(member));

                namespaceElement.Add(members);
            }

            return namespaceElement;
        }

        private static XElement PropertyConvert(CodeElement codeElement)
        {
            CodeProperty codeProperty = codeElement as CodeProperty;
            XElement propertyElement = new XElement("property");

            propertyElement.Add(DeclarationConvert(codeProperty.Declaration, "type"));

            XElement modifiers = ModifiersConvert(codeElement);
            if (modifiers != null)
                propertyElement.Add(modifiers);

            XElement variables = new XElement("variables", new XElement("variable", codeProperty.VariableNames.FirstOrDefault()));
            XElement accessors = new XElement("accessors");

            foreach(var accessor in codeProperty.Accessors)
                accessors.Add(new XElement("accessor", accessor));

            propertyElement.Add(variables);
            propertyElement.Add(accessors);
            return propertyElement;
        }

        private static XElement VariableConvert(CodeElement codeElement)
        {
            CodeField codeField = codeElement as CodeField;
            XElement fieldElement = new XElement("field");

            fieldElement.Add(DeclarationConvert(codeField.Declaration, "type"));

            XElement modifiers = ModifiersConvert(codeElement);
            if (modifiers != null)
                fieldElement.Add(modifiers);

            XElement variables = new XElement("variables");

            foreach (var variableName in codeField.VariableNames)
                variables.Add(new XElement("variable", variableName));

            fieldElement.Add(variables);
            
            return fieldElement;
        }

        private static XElement ConstructorConvert(CodeElement codeElement)
        {
            CodeConstructor codeConstructor = codeElement as CodeConstructor;
            XElement constructorElement = new XElement("constructor");

            constructorElement.Add(DeclarationConvert(codeConstructor.Declaration, "name"));

            XElement modifiers = ModifiersConvert(codeElement);
            if (modifiers != null)
                constructorElement.Add(modifiers);

            XElement parameters = MathodParamsConvert(codeConstructor.Parameters);
            if (parameters != null)
                constructorElement.Add(parameters);

            return constructorElement;
        }

        private static XElement DestructorConvert(CodeElement codeElement)
        {
            CodeDestructor codeDestructor = codeElement as CodeDestructor;
            XElement destructorElement = new XElement("destructor");

            destructorElement.Add(DeclarationConvert(codeDestructor.Declaration, "name"));

            return destructorElement;
        }

        private static XElement MethodConvert(CodeElement codeElement)
        {
            CodeMethod codeMethod = codeElement as CodeMethod;
            XElement methodElement = new XElement("method");

            methodElement.Add(DeclarationConvert(codeMethod.Declaration, "name"));
            methodElement.Add(DeclarationConvert(codeMethod.ReturnType, "returnType"));

            XElement modifiers = ModifiersConvert(codeElement);
            if (modifiers != null)
                methodElement.Add(modifiers);

            XElement parameters = MathodParamsConvert(codeMethod.Parameters);
            if (parameters != null)
                methodElement.Add(parameters);

            return methodElement;
        }

        private static XElement OperatorConvert(CodeElement codeElement)
        {
            CodeOperator codeOperator = codeElement as CodeOperator;
            XElement operatorElement = new XElement("operator");

            operatorElement.Add(DeclarationConvert(codeOperator.Declaration, "name"));
            operatorElement.Add(DeclarationConvert(codeOperator.ReturnType, "returnType"));


            XElement modifiers = ModifiersConvert(codeElement);
            if (modifiers != null)
                operatorElement.Add(modifiers);

            XElement parameters = MathodParamsConvert(codeOperator.Parameters);
            if (parameters != null)
                operatorElement.Add(parameters);

            return operatorElement;
        }

        private static XElement DelegateConvert(CodeElement codeElement)
        {
            CodeDelegate codeDelegate = codeElement as CodeDelegate;
            XElement delegateElement = new XElement("delegate");

            delegateElement.Add(DeclarationConvert(codeDelegate.Declaration, "name"));
            delegateElement.Add(DeclarationConvert(codeDelegate.ReturnType, "returnType"));

            XElement modifiers = ModifiersConvert(codeElement);
            if (modifiers != null)
                delegateElement.Add(modifiers);

            XElement parameters = MathodParamsConvert(codeDelegate.Parameters);
            if (parameters != null)
                delegateElement.Add(parameters);

            return delegateElement;
        }

        private static XElement EnumConvert(CodeElement codeElement)
        {
            CodeEnum codeEnum = codeElement as CodeEnum;
            XElement enumElement = new XElement("enum");

            enumElement.Add(new XAttribute("nrOfElements", codeEnum.Elements.Count));
            enumElement.Add(DeclarationConvert(codeEnum.Declaration, "name"));

            XElement modifiers = ModifiersConvert(codeElement);
            if (modifiers != null)
                enumElement.Add(modifiers);

            XElement items = new XElement("items");
            foreach (var item in codeEnum.Elements)
                items.Add(new XElement("item", item));

            enumElement.Add(items);
            return enumElement;
        }

        private static XElement TypeConvert(CodeElement codeElement)
        {
            CodeType codeType = codeElement as CodeType;
            XElement typeElement = new XElement(codeElement.Type.ToString().ToLower());

            typeElement.Add(DeclarationConvert(codeType.Declaration, "name"));

            XElement modifiers = ModifiersConvert(codeElement);
            if (modifiers != null)
                typeElement.Add(modifiers);

            if(codeType.Members.Count > 0)
            {
                XElement members = new XElement("members");

                foreach (var member in codeType.Members)
                    members.Add(ConvertToXml(member));

                typeElement.Add(members);
            }
            
            return typeElement;
        }

        #endregion

    }
}
