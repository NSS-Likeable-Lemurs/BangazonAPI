﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using Microsoft.AspNetCore.Http;
using Bangazon.Models;

namespace Bangazon.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public ProductsController(IConfiguration config)
        {
            _config = config;
        }

        public IDbConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        // GET api/students?q=Taco
        [HttpGet]
        public async Task<IActionResult> Get(string q)
        {
            string sql = @"
            SELECT
                p.Id,
                p.Price,
                p.Title,
                p.Description,
                p.Quantity,
                p.ProductTypeId,
                pt.Id,
                pt.Name
                FROM Product p
                JOIN ProductType pt ON p.ProductTypeId = pt.Id
            WHERE 1=1
            ";

            //    p.CustomerId
            //    c.Id,
            //    c.FirstName,
            //    c.LastName
            //JOIN Customer c ON p.CustomerId = c.Id

            if (q != null)
            {
                string isQ = $@"
                    AND p.Price LIKE '%{q}%'
                    OR p.Title LIKE '%{q}%'
                    OR p.Description LIKE '%{q}%'
                    OR p.Quantity LIKE '%{q}%'
                    OR p.ProductTypeId LIKE '%{q}%'
                ";
                sql = $"{sql} {isQ}";
            }

            Console.WriteLine(sql);

            using (IDbConnection conn = Connection)
            {

                IEnumerable<Product> products = await conn.QueryAsync<Product, ProductType, Product>(
                //,  Customer, 
                sql,
                    (product, productType) =>
                    {
                    product.ProductType = productType;
                //    product.Customer = customer;
                    return product;
                    }
                );
                return Ok(products);
            }
        }

        // GET api/products/5
        [HttpGet("{id}", Name = "GetProduct")]
        public async Task<IActionResult> Get([FromRoute]int id)
        {
            string sql = $@"
            SELECT
                p.Id,
                p.Price,
                p.Title,
                p.Description,
                p.Quantity,
                p.ProductTypeId
                FROM Product p
                WHERE p.Id = {id}
            ";
            //    p.CustomerId,
            //    c.Id,
            //    c.FirstName,
            //    c.LastName
            //JOIN Customer c ON p.CustomerId = c.Id

            using (IDbConnection conn = Connection)
            {
                IEnumerable<Product> products = await conn.QueryAsync<Product>(sql);
                return Ok(products.Single());
            }
        }

        // POST api/students
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Product product)
        {
            string sql = $@"INSERT INTO Product 
            (Price, Title, Description, Quantity, ProductTypeId)
            VALUES
            (
                '{product.Price}'
                ,'{product.Title}'
                ,'{product.Description}'
                ,'{product.Quantity}'
                ,'{product.ProductTypeId}'
            );
            SELECT SCOPE_IDENTITY();";

            //, CustomerId
            //,'{product.CustomerId}'

            using (IDbConnection conn = Connection)
            {
                var newId = (await conn.QueryAsync<int>(sql)).Single();
                product.Id = newId;
                return CreatedAtRoute("GetProduct", new { id = newId }, product);
            }
        }

        // PUT api/students/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Product product)
        {
            string sql = $@"
            UPDATE Product
            SET Price = '{product.Price}',
                Title = '{product.Title}',
                Description = '{product.Description}',
                Quantity = '{product.Quantity}'
            WHERE Id = {id}";

            try
            {
                using (IDbConnection conn = Connection)
                {
                    int rowsAffected = await conn.ExecuteAsync(sql);
                    if (rowsAffected > 0)
                    {
                        return new StatusCodeResult(StatusCodes.Status204NoContent);
                    }
                    throw new Exception("No rows affected");
                }
            }
            catch (Exception)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // DELETE api/students/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            string sql = $@"DELETE FROM Product WHERE Id = {id}";

            using (IDbConnection conn = Connection)
            {
                int rowsAffected = await conn.ExecuteAsync(sql);
                if (rowsAffected > 0)
                {
                    return new StatusCodeResult(StatusCodes.Status204NoContent);
                }
                throw new Exception("No rows affected");
            }

        }

        private bool ProductExists(int id)
        {
            string sql = $"SELECT Id FROM Product WHERE Id = {id}";
            using (IDbConnection conn = Connection)
            {
                return conn.Query<Product>(sql).Count() > 0;
            }
        }
    }

}