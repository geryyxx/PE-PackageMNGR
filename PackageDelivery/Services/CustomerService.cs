using PackageDelivery.Data;
using PackageDelivery.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PackageDelivery.Services
{
    public class CustomerService
    {
        private readonly AppDbContext _db;

        public CustomerService(AppDbContext db)
        {
            _db = db;
        }

        public Customer CreateCustomer(string name, string email, string phoneNumber)
        {
            name = name.Trim();
            email = email.Trim();
            phoneNumber = phoneNumber.Trim();

            ValidateCustomerData(name, email, phoneNumber);

            Customer customer = new Customer
            {
                Name = name,
                Email = email,
                PhoneNumber = phoneNumber
            };

            _db.Customers.Add(customer);
            _db.SaveChanges();

            return customer;
        }

        public List<Customer> GetAllCustomers()
        {
            return _db.Customers
                .OrderBy(c => c.Id)
                .ToList();
        }

        public Customer? GetCustomerById(int id)
        {
            return _db.Customers
                .FirstOrDefault(c => c.Id == id);
        }

        public List<Package> GetSentPackages(int customerId)
        {
            return _db.Packages
                .Where(p => p.SenderId == customerId)
                .OrderBy(p => p.ShippedAt)
                .ToList();
        }

        public List<Package> GetReceivedPackages(int customerId)
        {
            return _db.Packages
                .Where(p => p.RecipientId == customerId)
                .OrderBy(p => p.ShippedAt)
                .ToList();
        }

        public Customer UpdateCustomer(int id, string name, string email, string phoneNumber)
        {
            Customer? customer = GetCustomerById(id);

            if (customer == null)
                throw new ArgumentException("Nem található ilyen ügyfél.");

            name = string.IsNullOrWhiteSpace(name) ? customer.Name : name.Trim();
            email = string.IsNullOrWhiteSpace(email) ? customer.Email : email.Trim();
            phoneNumber = string.IsNullOrWhiteSpace(phoneNumber)
                ? customer.PhoneNumber : phoneNumber.Trim();

            ValidateCustomerData(name, email, phoneNumber);

            customer.Name = name;
            customer.Email = email;
            customer.PhoneNumber = phoneNumber;

            _db.SaveChanges();

            return customer;
        }

        private static void ValidateCustomerData(string name, string email, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A név nem lehet üres.");

            int atPosition = email.IndexOf('@');

            if (atPosition <= 0 || atPosition == email.Length - 1)
                throw new ArgumentException("Érvénytelen e-mail-cím.");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("A telefonszám nem lehet üres.");
        }
    }
}