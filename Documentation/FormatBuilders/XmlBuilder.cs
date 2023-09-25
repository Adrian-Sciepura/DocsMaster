using Documentation.Models;
using Documentation.Models.CodeElements;
using Documentation.Models.CodeElements.Methods;
using Documentation.Models.CodeElements.TypeKind;
using Documentation.Models.CodeElements.Types;
using Documentation.Models.CodeElements.Variables;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using TypeSetup = System.Collections.Generic.Dictionary<Documentation.Models.CodeElements.CodeElementType, bool>;

namespace Documentation.FormatBuilders
{
    internal class XmlBuilder : FormatBuilder
    {
        private static string _outputFolder;

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

        public override void Generate()
        {
            XElement root = new XElement("documentation");

            RunForEveryNamespace(_solutionTree.root, root);

            XDocument docsFile = new XDocument(root);
            docsFile.Save(Path.Combine(_outputFolder, "docs.xml"));
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

        private static XElement ModifiersConvert(CodeElement codeElement)
        {
            XElement modifiers = new XElement("modifiers");

            if(codeElement.AccessModifier == null)
                return modifiers;

            string[] modifierList = codeElement.AccessModifier.Split(' ');

            foreach (string modifier in modifierList)
                modifiers.Add(new XElement("modifier", modifier));

            return modifiers;
        }
        
        private static XElement MathodParamsConvert(List<CodeVariable> methodParameters)
        {
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
            
            XElement members = new XElement("members");

            foreach (var member in codeNamespace.InternalTypes)
                members.Add(ConvertToXml(member));

            namespaceElement.Add(members);
            return namespaceElement;
        }

        private static XElement PropertyConvert(CodeElement codeElement)
        {
            CodeProperty codeProperty = codeElement as CodeProperty;
            XElement propertyElement = new XElement("property");

            propertyElement.Add(DeclarationConvert(codeProperty.Declaration, "name"));
            propertyElement.Add(DeclarationConvert(codeProperty.Declaration, "type"));
            propertyElement.Add(ModifiersConvert(codeElement));

            XElement accessors = new XElement("accessors");

            foreach(var accessor in codeProperty.Accessors)
                accessors.Add(new XElement("accessor", accessor));

            return propertyElement;
        }

        private static XElement VariableConvert(CodeElement codeElement)
        {
            CodeVariable codeVariable = codeElement as CodeVariable;
            XElement variableElement = new XElement("variable");

            variableElement.Add(new XAttribute("name", codeVariable.FieldName));
            variableElement.Add(DeclarationConvert(codeVariable.Declaration, "type"));
            variableElement.Add(ModifiersConvert(codeElement));

            return variableElement;
        }

        private static XElement ConstructorConvert(CodeElement codeElement)
        {
            CodeConstructor codeConstructor = codeElement as CodeConstructor;
            XElement constructorElement = new XElement("constructor");

            constructorElement.Add(DeclarationConvert(codeConstructor.Declaration, "name"));
            constructorElement.Add(ModifiersConvert(codeElement));
            constructorElement.Add(MathodParamsConvert(codeConstructor.Parameters));

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
            methodElement.Add(ModifiersConvert(codeMethod));
            methodElement.Add(MathodParamsConvert(codeMethod.Parameters));

            return methodElement;
        }

        private static XElement OperatorConvert(CodeElement codeElement)
        {
            CodeOperator codeOperator = codeElement as CodeOperator;
            XElement operatorElement = new XElement("operator");

            operatorElement.Add(DeclarationConvert(codeOperator.Declaration, "name"));
            operatorElement.Add(DeclarationConvert(codeOperator.ReturnType, "returnType"));
            operatorElement.Add(ModifiersConvert(codeOperator));
            operatorElement.Add(MathodParamsConvert(codeOperator.Parameters));

            return operatorElement;
        }

        private static XElement DelegateConvert(CodeElement codeElement)
        {
            CodeDelegate codeDelegate = codeElement as CodeDelegate;
            XElement delegateElement = new XElement("delegate");

            delegateElement.Add(DeclarationConvert(codeDelegate.Declaration, "name"));
            delegateElement.Add(DeclarationConvert(codeDelegate.ReturnType, "returnType"));
            delegateElement.Add(ModifiersConvert(codeDelegate));
            delegateElement.Add(MathodParamsConvert(codeDelegate.Parameters));

            return delegateElement;
        }

        private static XElement EnumConvert(CodeElement codeElement)
        {
            CodeEnum codeEnum = codeElement as CodeEnum;
            XElement enumElement = new XElement("enum");

            enumElement.Add(new XAttribute("nrOfElements", codeEnum.Elements.Count));
            enumElement.Add(DeclarationConvert(codeEnum.Declaration, "name"));

            foreach (var item in codeEnum.Elements)
                enumElement.Add(new XElement("item", item));
            
            return enumElement;
        }
        private static XElement TypeConvert(CodeElement codeElement)
        {
            CodeType codeType = codeElement as CodeType;
            XElement typeElement = new XElement(codeElement.Type.ToString().ToLower());

            typeElement.Add(DeclarationConvert(codeType.Declaration, "name"));
            typeElement.Add(ModifiersConvert(codeElement));

            XElement members = new XElement("members");

            foreach(var member in codeType.Members)
                members.Add(ConvertToXml(member));

            typeElement.Add(members);
            return typeElement;
        }

        #endregion

    }
}
