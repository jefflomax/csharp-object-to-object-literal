using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace profdata.WF.Services.IntegrationTests.TestHelpers
{
	public class Restriction
	{
		// TODO: Add Properties by RegEx
		public string EntityName { get; set; }
		private HashSet<string> IncludeProperties { get; set; }
		private HashSet<string> ExcludeProperties { get; set; }

		public Restriction
		(
			string entityName,
			string includeProperties,
			string excludeProperties
		)
		{
			if( includeProperties.Length > 0 && excludeProperties.Length > 0)
				throw new Exception("Restriction Include and Exclude property lists are mutually exclusive.");
			
			EntityName = entityName;
			IncludeProperties = (includeProperties.Length > 0)
				? new HashSet<string>(includeProperties.Split(','))
				: new HashSet<string>();
			ExcludeProperties = (excludeProperties.Length > 0)
				? new HashSet<string>(excludeProperties.Split(','))
				: new HashSet<string>();
		}

		public bool Exclude(string propertyName)
		{
			if (ExcludeProperties.Count == 0)
				return false;

			return ExcludeProperties.Contains(propertyName);
		}

		public bool Include(string propertyName)
		{           
			if (IncludeProperties.Count == 0)
				return true;

			return IncludeProperties.Contains(propertyName);
		}

		public bool SkipEntireEntity()
		{
			return (IncludeProperties.Count + ExcludeProperties.Count) == 0;
		}
	}

	public static class ObjectToObjectLiteral
	{
		public static string NewLine = Environment.NewLine;
		public static string KeyProperty = "Key";

		public class Entity
		{
			public Type Type { get; set; }
			public long Key { get; set; }
			public string Path { get; set; }

			public Entity(object obj, string path)
			{
				Type = obj.GetType();
				var keyProperty = Type.GetProperty(ObjectToObjectLiteral.KeyProperty);
				// TODO: Support the data type (int, long, GUID) for the keys
				Key = (keyProperty == null)
					? 0
					: Convert.ToInt64(keyProperty.GetValue(obj));
				Path = (path.Length == 0)
					? UniqueName
					: path;
			}

			public string UniqueName 
			{ 
				get { return string.Format("{0}{1}", Type.Name, Key); }
			}
		}

		public class EntityEqualityComparer : IEqualityComparer<Entity>
		{
			public bool Equals( Entity lhs, Entity rhs )
			{
				if (lhs == null && rhs == null)
					return true;

				if (lhs == null || rhs == null)
					return false;

				return lhs.Type == rhs.Type && lhs.Key == rhs.Key;
			}

			public int GetHashCode( Entity entity)
			{
				return ( entity == null )
					? 0
					: entity.Type.GetHashCode() + entity.Key.GetHashCode();
			}
		}

#if false
		public class ObjectEqualityComparer : IEqualityComparer<Object>
		{
			new public bool Equals(object lhs, object rhs)
			{
				return ReferenceEquals(lhs, rhs);
			}

			public int GetHashCode(object obj)
			{
				return obj.GetHashCode();
			}
		}
#endif



		public static string GetLaborScheduleGlobalExcludesProperties()
		{
			return "IsPersistent,Timestamp,ImportBatch";
		}


		public static Restriction[] GetLaborSchedulerCommonRestrictions()
		{
			return new Restriction[]
			{
				new Restriction( "OrganizationChart", "Key",""),
				new Restriction
				(
					"WorkType",
					"Key,Code,Description,IsPaid,ExcludeTimeFromDashboard,IsRestrictedByBusinessEntity,IsRestrictedByProfitCenter",
					""
				)
			};
		}

		public static string GetPayrollGlobalRestrictions()
		{
			return "County,EmployeeOrganizations,IsPersistent,Organization,OrganizationLevel,Vendor";
		}

		public static Restriction[] GetPayrollCommonRestrictions()
		{
			return new Restriction[]
			{
				new Restriction
				(
					"CompanyUser",
					"Id,Description,EmailAddress,Key",
					""
				),
				new Restriction
				(
					"Deduction",
					"",
					"DataIdKey,ExcludeFromSupplementalPayroll,GlAccountMajor,IncludeInW2Box12,IncludeInW2Box14,IncludeInW2Box14Description,PostToAccountPayable,CreateOneInvoicePerEmployee,NumberOfDigits,RemittanceComment,InternalComment,IsPersistent"
				), 
				new Restriction
				(
					"Employee",
					"Key,FirstName,LastName,Identity",
					""
				),
				new Restriction
				(
					"EmployeeDeduction",
					"",
					"RemittanceComment,InternalComment,CaseIdentifier,ParentName,ParentNationalId,HasMedicalSupport"
				),
				new Restriction
				(
					"EmployeeIdentity",
					"Key,Id,Alias",
					""
				),
				new Restriction( "PayGroup", "Key,Value,Description",""),
				new Restriction( "PayrollTax", "Key,PayrollTaxId,PayrollTaxName",""),
				new Restriction( "PayrollTaxGeocode", "Key,Geocode",""),
				new Restriction( "Site", "Key","")
			};
		}

		
		/// <summary>
		/// Convert NHibernate .NET object graph to C# Object Literal Contructor
		/// 
		/// </summary>
		/// <param name="obj">Object graph to serialize to C# Object Literal Constructor</param>
		/// <param name="globalExcludeProperties">Properties to globally exculde</param>
		/// <param name="entityRestrictions">Entities to serialize as a limited set of properties, useful to limit depth of traversal</param>
		/// <returns>string containing Object Literal Constructor</returns>
		public static string ToObjectInitializer
		(
			Object obj,
			string globalExcludeProperties,
			params Restriction[] entityRestrictions
		)
		{
			var sb = new StringBuilder(1024);

			// Maintain a list of each entity encountered.  This is used to
			// prevent "parent" or duplicate references from causing an
			// infinite loop.  If the entity is a parent relationship, NHibernate
			// will have the same reference on both objects, but if it is simply
			// a second reference it will have a different reference
			var hist = new Dictionary<Entity, Entity>(new EntityEqualityComparer());

			// Place Entity Names with Property limitations in a dictionary
			Dictionary<string, Restriction> restrictions = entityRestrictions
				.ToDictionary(k => k.EntityName, v => v);

			// Place properties to globally exclude in a HashSet
			string [] excludeProperties = globalExcludeProperties.Split(',');
			var exclude = new HashSet<string>(excludeProperties);

#if false
			sb.Append(" // Object Literal " + NewLine);

			if (excludeProperties.Length > 0)
			{
				sb.Append("// Globally Excluded Properties:" + NewLine);
				sb.Append("//  " + globalExcludeProperties + NewLine);
			}

			if (restrictions.Count > 0) 
			{
				// Comment the restricted entities
				sb.Append("// Restricted Entities:" + NewLine);
				foreach (var restriction in entityRestrictions)
				{
					sb.AppendFormat(@"// {0}{1}", restriction.EntityName, NewLine);
					sb.AppendFormat
					(
						@"//  Allowed Properties: {0}{1}",
						String.Join(",", restriction.IncludeProperties),
						NewLine
					);
				}
			}
#endif
			var rootEntity = new Entity(obj, string.Empty);

			sb.Append("var ");
			WalkObject
			(
				obj,
				sb,
				hist,
				restrictions,
				exclude,
				level: 0,
				parent: rootEntity.UniqueName
			);

			return sb.ToString();
		}

		/// <summary>
		/// Create string of 0 or more Tab characters
		/// </summary>
		/// <param name="count">count of Tabs</param>
		/// <returns></returns>
		private static string Tabs(int count)
		{
			return (count == 0)
				? string.Empty
				: new String('\t', count);
		}

		/// <summary>
		/// Walk a NHibernate hydrated .NET object graph, returning a C# 3.0 Object Literal
		/// Constructor of public properties.  Parent and duplicate entity references are
		/// restricted to prevent infinite loops
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="sb"></param>
		/// <param name="hist"></param>
		/// <param name="restrictions"></param>
		/// <param name="exclude"></param>
		/// <param name="level"></param>
		/// <param name="parent"></param>
		/// <returns></returns>
		private static void WalkObject
		(
			Object obj,
			StringBuilder sb,
			Dictionary<Entity, Entity> hist,
			Dictionary<string, Restriction> restrictions,
			HashSet<string> exclude,
			int level,
			string parent
		)
		{
			if (obj == null)
			{
				sb.AppendFormat("{0} = null;{1}", parent, NewLine);
				return;
			}

			var type = obj.GetType();
			var properties = type.GetProperties();
			var workingTypeName = type.Name;

			var entity = new Entity(obj, parent);

			// NHibernate uses EntityNameProxy to lazily hydrate objects
			if (workingTypeName.EndsWith("Proxy"))
			{
				workingTypeName = workingTypeName.Substring(0, workingTypeName.Length - "Proxy".Length);
			}
			if (workingTypeName.EndsWith("Proxy2"))
			{
				workingTypeName = workingTypeName.Substring(0, workingTypeName.Length - "Proxy2".Length);
			}


#if false
			// Company specific code
			// This class can't be constructed with an Object Literal, the old code
			// emitted a constructor, this code looks up the static field value
			// of the enum.
			if (type.BaseType.FullName.Equals("profdata.LI.Utils.DataType.Extensions.Enumeration"))
			{
				var enumValue = type.GetProperty("Value").GetValue(obj);
				var enumName = type.GetProperty("DisplayName").GetValue(obj);
				
				var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
				foreach (var field in fields)
				{
					var fieldEnum = field.GetValue(null);
					var fieldValuePropertyInfo = fieldEnum.GetType().GetProperty("Value");
					var fieldValue = fieldValuePropertyInfo.GetValue(fieldEnum);
					if (Convert.ToInt32(enumValue) == Convert.ToInt32(fieldValue))
					{
						var fieldName = field.Name;
						sb.AppendFormat("{0} = {1}.{2};{3}", parent, workingTypeName, fieldName, NewLine);
						return;
					}
				}

				//sb.AppendFormat("{0} = new {1}({2},\"{3}\");{4}", parent, workingTypeName, enumValue, enumName,NewLine);
				//return;
			}
#endif

			Entity canonicalEntity;
			if (hist.TryGetValue(entity, out canonicalEntity))
			{
				// Duplicate Entity, emit as Key only
				System.Diagnostics.Debug.WriteLine("Already Referenced Object " + workingTypeName + ": " + entity.Key);
				
				sb.AppendFormat
				(
					"{0} = {1};{2}",
					parent,
					canonicalEntity.Path,
					NewLine
				);
				return;
			}

			bool isRestricted = restrictions.ContainsKey(workingTypeName);
			var restriction = (isRestricted)
				? restrictions[workingTypeName]
				: null;

			bool skipEntity = (isRestricted) && restriction.SkipEntireEntity();

			if (skipEntity) // TODO: Not Tested
				return;

			System.Diagnostics.Debug.WriteLine("Adding Entity " + workingTypeName + ":" + entity.Key);
			hist.Add(entity,entity);

			// Emit Object with all base type properties
			// TODO: If list is base type, include here
			sb.AppendFormat("{0} = new {1} {{", parent, workingTypeName);

			bool appendComma = false;
			foreach (var property in properties.OrderBy(p => p.Name == "Key" ? "!!!" : p.Name == "Timestamp" ? "zzz" : p.Name))
			{
				if( SkipProperty(property,isRestricted,restriction,exclude))
				{
					continue;
				}

				var pt = property.PropertyType;
				var propertyName = property.Name;
				var name = pt.Name;

				if( IsBaseType(property))
				{
					//System.Diagnostics.Debug.WriteLine("BASE Property:{0} Type:{1}", propertyName, name);
					if (appendComma)
					{
						sb.Append("," + NewLine + Tabs(level));
					}
					else
					{
						sb.Append(NewLine + Tabs(level));
						appendComma = true;
					}
					HandleBaseTypes(property, obj, sb, level);
				}
			}

			sb.AppendLine("};");

			// Emit all class type and lists, assiging into the parent class/path
			foreach (var property in properties.OrderBy(p => p.Name))
			{
				if (SkipProperty(property, isRestricted, restriction, exclude))
				{
					continue;
				}

				var pt = property.PropertyType;
				var propertyName = property.Name;
				var name = pt.Name;

				if (!IsBaseType(property))
				{
					System.Diagnostics.Debug.WriteLine("CLASS Property:{0} Type:{1}", propertyName, name);
					var interfaces = property.PropertyType.GetInterfaces();
					var isList = interfaces.Contains(typeof(IList));
					var isEnumerable = property.PropertyType.FullName.StartsWith("System.Collections.Generic.IEnumerable");
					var isGenericIList = property.PropertyType.FullName.StartsWith("System.Collections.Generic.IList");
					//IEnumerable enumerableTest = property.GetValue(obj, null) as IEnumerable;
					//var isCollection = IsCollection(obj);
					//var isClass = property.PropertyType.IsClass;
					//var isGeneric = obj.GetType().IsGenericType;

					if (isList || isGenericIList || isEnumerable)
					{
						Object listObj = property.GetValue(obj, null);
						var list = (IList)property.GetValue(obj, null);
						//var collection = (ICollection)property.GetValue(obj, null);
						//var xx = property.PropertyType.UnderlyingSystemType;

						if (property.PropertyType.BaseType == typeof(System.Array))
						{
							var listTypeName = property.PropertyType.Name; // includes []
							string listParent = string.Format("{0} = new {1}", ParentPath(parent, property.Name), listTypeName);
#if false
							sb.AppendFormat
							(
								"{0}.{1} = new {2}{3}{4}{{{5}{6}",
								parent + "." + entity.UniqueName,
								property.Name,
								listTypeName,
								NewLine,
								Tabs(level),
								NewLine,
								Tabs(level + 1)
							);
#endif
							WalkList(list, sb, hist, restrictions, exclude, level + 1, ParentPath(parent, propertyName), listParent);

						}
						else if (list != null && list.Count > 0)
						{
							var listTypeName = property.PropertyType.GetGenericArguments()[0].Name;
							string listParent = string.Format("{0} = new List<{1}>", ParentPath(parent, property.Name), listTypeName);
#if false
							sb.AppendFormat
							(
								"{0}{1} = new List<{2}>{3}{4}{{{5}{6}",
								parent,
								property.Name,
								listTypeName,
								NewLine,
								Tabs(level),
								NewLine,
								Tabs(level + 1)
							);
#endif
							WalkList(list, sb, hist, restrictions, exclude, level + 1, ParentPath(parent,propertyName), listParent);
						}
						else
						{
							var listTypeName = property.PropertyType.GetGenericArguments()[0].Name;
							sb.AppendFormat
							(
								"{0}.{1} = new List<{2}>();{3}",
								parent,
								property.Name,
								listTypeName,
								NewLine
							);
						}
					}
					else
					{
						WalkObject(property.GetValue(obj), sb, hist, restrictions, exclude, level + 1, ParentPath( parent, propertyName));
					}

				}
			}
//			sb.Append(NewLine);
		}// WalkObject

		private static string ParentPath( string parent, string propertyName )
		{
			return (parent.Length == 0)
				? propertyName
				: parent + "." + propertyName ;
		}

		private static bool SkipProperty( PropertyInfo property, bool isRestricted, Restriction restriction, HashSet<string> globalExcludes)
		{
			if (globalExcludes.Contains(property.Name))
			{
				return true;
			}
			
			if (!isRestricted)
				return false;

			if (restriction.Exclude(property.Name))
				return true;

			return ! restriction.Include(property.Name);
		}

		// System types handled as primitives
		private static HashSet<System.Type> baseTypes = new HashSet<System.Type>
		(
			new System.Type[]
			{
				typeof(String),
				typeof(Int16), typeof(Int32), typeof(Int64),
				typeof(Single), typeof(Double),
				typeof(Decimal),
				typeof(Boolean),
				typeof(DateTime),
				typeof(byte[])
			}
		);

		private static bool IsBaseType( PropertyInfo property )
		{
			var pt = property.PropertyType;

			if (IsCompanySpecificBaseType(pt))
			{
				return true;
			}

			if (baseTypes.Contains(pt))
			{
				return true;
			}

			if (pt.IsEnum)
			{
				return true;
			}

			var name = pt.Name;
			var isNullable = pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(Nullable<>);
			if (isNullable)
			{
				var underlyingSystemType = pt.GetGenericArguments()[0].UnderlyingSystemType;

				if( baseTypes.Contains(underlyingSystemType))
					return true;

				name = pt.GetGenericArguments()[0].Name;
			}

			// TODO: Remove the string check
			switch (name)
			{
				case "Int16":
				case "Int32":
				case "Int64":
				case "Single":
				case "Double":
				case "Decimal":
				case "Boolean":
				case "DateTime":
				case "String":
					return true;

				default:
					return false;
			}
		}

		private static bool IsCompanySpecificBaseType(Type type)
		{
			// We use an internal "Enumeration" class that cannot be constructed but
			// can be treated as a base type
			if (type.BaseType == null)
				return false;

			return type.BaseType.FullName.Equals("profdata.LI.Utils.DataType.Extensions.Enumeration");
		}


		private static bool HandleBaseTypes(PropertyInfo property, Object obj, StringBuilder sb, int level)
		{
			DateTime workDt;
			string workStr;
			bool boolWork = false;
			var pt = property.PropertyType;
			var name = pt.Name;
			var value = property.GetValue(obj);

			if (IsCompanySpecificBaseType(pt))
			{
				HandleCompanySpecificBaseType(property.Name, value, sb, level);
				return true;
			}

			var isNullable = pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(Nullable<>);
			if (isNullable)
			{
				name = pt.GetGenericArguments()[0].Name;
				if (property.GetValue(obj) == null)
				{
					sb.AppendFormat("{0} = null", property.Name);
					return true;
				}
			}

			if (pt == typeof (byte[]))
			{
				var ba = (byte[]) value;
				var s = String.Join(",", ba.Select(b => b.ToString() ));

				sb.AppendFormat("{0} = new byte[]{{{1}}}", property.Name, s);
				return true;
			}

			if (pt.IsEnum)
			{
				sb.AppendFormat("{0} = {1}", property.Name, property.GetValue(obj));
				return true;
			}


			switch (name)
			{
				case "Int16":
				case "Int32":
					sb.AppendFormat("{0} = {1}", property.Name, value);
					return true;

				case "Int64":
					sb.AppendFormat("{0} = {1}L", property.Name, value);
					return true;

				case "Single":
					sb.AppendFormat("{0} = {1}F", property.Name, value);
					return true;

				case "Double":
					sb.AppendFormat("{0} = {1}D", property.Name, value);
					return true;

				case "Decimal":
					sb.AppendFormat("{0} = {1}m", property.Name, value);
					return true;

				case "Boolean":
					try
					{
						boolWork = Convert.ToBoolean(value);
					}
					catch (Exception)
					{
						boolWork = false;
					}
					sb.AppendFormat("{0} = {1}", property.Name, (boolWork) ? "true" : "false");
					return true;

				case "DateTime":
					workDt = Convert.ToDateTime(value);
					if (workDt.Hour == 0 && workDt.Minute == 0 && workDt.Second == 0)
					{
						sb.AppendFormat
						(
							"{0} = new DateTime({1},{2},{3})",
							property.Name,
							workDt.Year,
							workDt.Month,
							workDt.Day
						);
					}
					else
					{
						sb.AppendFormat
						(
							"{0} = new DateTime({1},{2},{3},{4},{5},{6})",
							property.Name,
							workDt.Year,
							workDt.Month,
							workDt.Day,
							workDt.Hour,
							workDt.Minute,
							workDt.Second
						);
					}
					return true;

				case "String":
					// TODO: Just prefix string with @?
					workStr = Convert.ToString(value)
						.Replace(@"\", @"\\")
						.Replace("\"", "\\\"")
						.Replace("\r", "\\r")
						.Replace("\n", "\\n");

					sb.AppendFormat("{0} = \"{1}\"", property.Name, workStr);
					return true;

				// Byte, SByte, Char, TimeSpan ?
			}

			return false;
		}

		private static void HandleCompanySpecificBaseType(string propertyName, object obj, StringBuilder sb, int level)
		{
			if (obj == null)
			{
				sb.AppendFormat("{0} = null /* Null Enum */", propertyName);
				return;
			}

			var type = obj.GetType();
			var workingTypeName = type.Name;


			var enumValue = type.GetProperty("Value").GetValue(obj);
			var enumName = type.GetProperty("DisplayName").GetValue(obj);

			var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
			foreach (var field in fields)
			{
				var fieldEnum = field.GetValue(null);
				var fieldValuePropertyInfo = fieldEnum.GetType().GetProperty("Value");
				var fieldValue = fieldValuePropertyInfo.GetValue(fieldEnum);
				if (Convert.ToInt32(enumValue) == Convert.ToInt32(fieldValue))
				{
					var fieldName = field.Name;
					sb.AppendFormat("{0} = {1}.{2}", propertyName, workingTypeName, fieldName);
					return;
				}
			}

		} // HandleBaseTypes


		private static void WalkList
		(
			IList list,
			StringBuilder sb,
			Dictionary<Entity, Entity> hist,
			Dictionary<string, Restriction> restrictions,
			HashSet<string> exclude,
			int level,
			string parent,
			string listParent
		)
		{
			//sb.AppendLine("");
			var listEntities = new List<Entity>();
			foreach( object obj in list )
			{
				// TODO: Lookup to see if this entity has ref'ed
				var listEntity = new Entity(obj, string.Empty);
				listEntities.Add(listEntity);

				sb.Append("var ");
				WalkObject
				(
					obj,
					sb,
					hist,
					restrictions,
					exclude,
					level,
					parent: listEntity.UniqueName
				);
			}

			bool appendComma = false;
			sb.Append(listParent + " {");
			foreach (var listEntity in listEntities)
			{
				if (appendComma)
				{
					sb.Append(",");
				}
				appendComma = true;

				sb.Append(listEntity.UniqueName);
			}
			sb.Append("};"+ NewLine);
		}// WalkList
	}// class ObjectToObjectLiteral
}
 
