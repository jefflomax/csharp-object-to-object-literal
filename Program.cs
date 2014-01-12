using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ObjectLiteral
{
	public class Program
	{
		static void Main(string[] args)
		{
			Order order = GetOrder();

			string result = ObjectToObjectLiteral.ToObjectInitializer
			(
				order,
				globalExcludeProperties: "Description",
				entityRestrictions: new Restriction[0]
			);

			
			WriteToFile("test.txt", result);
			Console.WriteLine(result);

			// Poor man's unit test, paste results into 
			// GetOrderFromObjectListeral()
			if( CompareOrders( order, GetOrderFromObjectLiteral()) )
			{
				Console.WriteLine( "Object Literal matches original");
			}
			else
			{
				Console.WriteLine( "Error: Object Literal does not match");
			}
			Console.ReadLine();
		}

		private static void WriteToFile(string fileName, string content)
		{
			System.IO.File.WriteAllText(fileName, content);
		}

		private static bool CompareOrders( Order lhs, Order rhs )
		{
			var itemDifferences = lhs.Items.Except(rhs.Items,new ItemEqualityComparer());
			if( itemDifferences.Any())
				return false ;

			if( rhs.Items[0].Product.Category.Departments[0].Description != null )
			{
				return false ; // Property should have been skipped
			}

			return lhs.Key == rhs.Key &&
				lhs.Items.Count == rhs.Items.Count &&
				lhs.Address.Key == rhs.Address.Key ;
		}

		public static Order GetOrder()
		{
			Customer customer = new Customer
			{
				Key = 1,
				FirstName = "Mortimer",
				LastName = "Sneurd",
				Timestamp = "3"
			};

			Address homeAddress = new Address
			{
				Key = 0,
				Customer = customer,
				Street = "1313 Mockingbird Lane",
				City="Candy Land",
				AddressType = AddressType.Residential,
				ZipCode= 90210,
				Timestamp="1"
			};

			Address businessAddress = new Address
			{
				Key = 1,
				Customer = customer,
				Street = "411 Bonham",
				City = "Sao Antonio",
				AddressType = AddressType.Business,
				ZipCode = 66016,
				Timestamp = "2"
			};

			customer.Addresses = new List<Address> { homeAddress, businessAddress };

			Department department1 = new Department { Key = 0, Name = "Electronics", Timestamp ="4" };
			Department department2 = new Department { Key = 1, Name = "Computers", Timestamp = "5" };
			Department department3 = new Department { Key = 2, Name = "Music", Timestamp = "6" };

			Department[] batteryDepartments =
				new Department[] { department1, department2 };
			
			Category category1 = new Category 
			{
				Key = 0,
				Name = "Batteries",
				SortOrder = 1,
				Departments = batteryDepartments,
				Timestamp = "7"
			};
			Category category2 = new Category
			{
				Key = 2,
				Name = "Sheet Music",
				SortOrder = 2,
				Departments = new Department[2] { department2, department3 },
				Timestamp = "8"
			};

			Product product1 = new Product { Key = 0, Name = "Everready", Category = category1, Timestamp = "9" };
			Product product2 = new Product { Key = 1, Name = "Shroeder's Theme", Category = category2, Timestamp = "10" };

			// Add a parent ref to 
			Item item1 = new Item { Key = 0, Price = 13.13F, Quantity = 6, Product = product1, Timestamp = "11" };
			Item item2 = new Item { Key = 1, Price = 15.00F, Quantity = 1, Product = product2, Timestamp = "12" };
			Item item3 = new Item { Key = 2, Price = 5.00F, Quantity = 1, Product = product1, Timestamp = "13" };

			Order order = new Order
			{
				Key = 1,
				Name = "Business Order #1",
				Customer = customer,
				Address = businessAddress,
				Items = new List<Item>{ item1, item2, item3},
				SentDate = null,
				Hold = true,
				Coupons = new List<String> { "Summer", "Fall"},
				Alerts = new long[] { 1234, 5678 },
				ContactTimes = new DateTime[] { new DateTime(2013,12,25,09,30,0), new DateTime(2014,1,1,10,0,0)},
				Timestamp = "14"//,
				//Chars = new List<char?> { 'a', null, 'b', 'c' } // Not supported
			};

			item1.Order = order;
			item2.Order = order;
			item3.Order = order;

			return order;
		}

		/// <summary>
		/// Paste the output of the Object Literal Serializer here
		/// </summary>
		/// <returns></returns>
		private static Order GetOrderFromObjectLiteral()
		{
			// Object Literal
			// Globally Excluded Properties:
			//  Description
			var Order1 = new Order
			{
				Name = "Business Order #1",
				SentDate = null,
				Hold = true,
				Key = 1L,
				Timestamp = "14"
			};
			Order1.Customer = new Customer
			{
				FirstName = "Mortimer",
				LastName = "Sneurd",
				Key = 1L,
				Timestamp = "3"
			};
			var Address0 = new Address
			{
				Street = "1313 Mockingbird Lane",
				City = "Candy Land",
				AddressType = AddressType.Residential,
				ZipCode = 90210,
				Key = 0L,
				Timestamp = "1"
			};
			Address0.Customer = Order1.Customer;
			var Address1 = new Address
			{
				Key = 1L,
				Street = "411 Bonham",
				City = "Sao Antonio",
				AddressType = AddressType.Business,
				ZipCode = 66016,
				Timestamp = "2"
			};
			Address1.Customer = Order1.Customer;
			Order1.Customer.Addresses = new List<Address> { Address0, Address1 };
			Order1.Address = Address1;
			var Item0 = new Item
			{
				Quantity = 6,
				Price = 13.13F,
				Key = 0L,
				Timestamp = "11"
			};
			Item0.Order = Order1;
			Item0.Product = new Product
			{
				Name = "Everready",
				Key = 0L,
				Timestamp = "9"
			};
			Item0.Product.Category = new Category
			{
				Name = "Batteries",
				SortOrder = 1,
				Key = 0L,
				Timestamp = "7"
			};
			var Department0 = new Department
			{
				Name = "Electronics",
				Key = 0L,
				Timestamp = "4"
			};
			var Department1 = new Department
			{
				Key = 1L,
				Name = "Computers",
				Timestamp = "5"
			};
			Item0.Product.Category.Departments = new Department[] { Department0, Department1 };
			var Item1 = new Item
			{
				Key = 1L,
				Quantity = 1,
				Price = 15F,
				Timestamp = "12"
			};
			Item1.Order = Order1;
			Item1.Product = new Product
			{
				Name = "Shroeder's Theme",
				Key = 1L,
				Timestamp = "10"
			};
			Item1.Product.Category = new Category
			{
				Name = "Sheet Music",
				SortOrder = 2,
				Key = 2L,
				Timestamp = "8"
			};
			var Department2 = new Department
			{
				Key = 2L,
				Name = "Music",
				Timestamp = "6"
			};
			Item1.Product.Category.Departments = new Department[] { Department1, Department2 };
			var Item2 = new Item
			{
				Key = 2L,
				Quantity = 1,
				Price = 5F,
				Timestamp = "13"
			};
			Item2.Order = Order1;
			Item2.Product = Item0.Product;
			Order1.Items = new List<Item> { Item0, Item1, Item2 };
			Order1.Alerts = new Int64[] { 1234L, 5678L };
			Order1.ContactTimes = new DateTime[] { new DateTime(2013, 12, 25, 9, 30, 0), new DateTime(2014, 1, 1, 10, 0, 0) };
			Order1.Coupons = new List<String> { "Summer", "Fall" };

			return Order1;
		}
	}

	/***********************/
	/*    Test Classes     */
	/***********************/

	public class PersistantObject
	{
		public long Key { get; set; }
		public string Timestamp { get; set;}
	}

	public enum AddressType { Business, Residential };

	public class Address : PersistantObject
	{
		public Customer Customer { get; set; }
		public string Street { get; set;}
		public string City { get; set; }
		public AddressType AddressType { get; set;}
		public int ZipCode { get; set; }
	}

	public class Customer : PersistantObject
	{
		public string FirstName { get; set;}
		public string LastName { get;set;}
		public List<Address> Addresses {get; set;}
	}

	public class Department : PersistantObject
	{
		public string Name { get; set; }
		public string Description { get; set;}
	}

	public class Category : PersistantObject
	{
		public string Name { get; set; }
		public short SortOrder { get; set; }
		public Department[] Departments { get; set; }
	}

	public class Product : PersistantObject
	{
		public string Name { get; set; }
		public Category Category { get; set; }
	}
	public class ProductEqualityComparer : IEqualityComparer<Product>
	{
		public bool Equals(Product lhs, Product rhs)
		{
			return lhs.Key == rhs.Key &&
				lhs.Timestamp == rhs.Timestamp &&
				lhs.Name.Equals( rhs.Name ) &&
				lhs.Category.Key == rhs.Category.Key &&
				lhs.Category.Departments.Length == rhs.Category.Departments.Length;
		}
		public int GetHashCode(Product product)
		{
			return product.Key.GetHashCode();
		}
	}

	public class Item : PersistantObject
	{
		public Order Order {  get; set; }
		public int Quantity { get; set; }
		public float Price { get; set; }
		public Product Product { get; set; }
	}
	public class ItemEqualityComparer : IEqualityComparer<Item>
	{
		public bool Equals( Item lhs, Item rhs )
		{
			var productEqualityComparer = new ProductEqualityComparer();

			return lhs.Key == rhs.Key &&
				lhs.Price == rhs.Price &&
				lhs.Order.Key == rhs.Order.Key &&
				lhs.Quantity == rhs.Quantity &&
				lhs.Timestamp == rhs.Timestamp &&
				productEqualityComparer.Equals(lhs.Product,rhs.Product);
		}
		public int GetHashCode( Item item )
		{
			return item.Key.GetHashCode();
		}
	}

	public class Order : PersistantObject
	{
		public string Name { get; set; }
		public Customer Customer { get; set; }
		public Address Address { get; set; }
		public List<Item> Items { get; set; }
		public Nullable<DateTime> SentDate { get; set; }
		public long[] Alerts { get; set; }
		public DateTime[] ContactTimes { get; set; }
		public List<String> Coupons { get; set; }
		public bool Hold { get; set; }
		// public List<char?> Chars { get; set; } Not Supported
	}

}
