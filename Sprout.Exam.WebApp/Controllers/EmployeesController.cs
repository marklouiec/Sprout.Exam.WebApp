using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Sprout.Exam.Business.DataTransferObjects;
using Sprout.Exam.Common.Enums;
using Sprout.Exam.WebApp.Data;
using Sprout.Exam.DataAccess.Entities;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Sprout.Exam.WebApp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmployeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Refactor this method to go through proper layers and fetch from the DB.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await Task.FromResult(_context.Employee.Where(m => m.IsDeleted == false).Select(m => new EmployeeDto
            {
                Birthdate = m.Birthdate.ToString("yyyy-MM-dd"),
                TypeId = m.EmployeeTypeId,
                FullName = m.FullName,
                Id = m.Id,
                Tin = m.Tin
            }).ToList());
            return Ok(result);
        }

        /// <summary>
        /// Refactor this method to go through proper layers and fetch from the DB.
        /// </summary>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await Task.FromResult(_context.Employee.FirstOrDefault(m => m.Id == id));
            var employee = new EmployeeDto()
            {
                Birthdate = result.Birthdate.ToString("yyyy-MM-dd"),
                FullName = result.FullName,
                Id = result.Id,
                Tin = result.Tin,
                TypeId = result.EmployeeTypeId
            };
            return Ok(employee);
        }

        /// <summary>
        /// Refactor this method to go through proper layers and update changes to the DB.
        /// </summary>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(EditEmployeeDto input)
        {
            var employee = _context.Employee.FirstOrDefault(n => n.Id == input.Id);
            employee.FullName = input.FullName;
            employee.Birthdate = input.Birthdate;
            employee.Tin = input.Tin;
            employee.EmployeeTypeId = input.TypeId;

            _context.SaveChanges();


            return Ok(employee);
        }

        /// <summary>
        /// Refactor this method to go through proper layers and insert employees to the DB.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Post(CreateEmployeeDto input)
        {

            var newEmployee = new EmployeeEntity()
            {
                Birthdate = input.Birthdate,
                EmployeeTypeId = input.TypeId,
                FullName = input.FullName,
                Tin = input.Tin,
            };

            _context.Employee.Add(newEmployee);
            _context.SaveChanges();

            return Created($"/api/employees/{newEmployee.Id}", newEmployee.Id);
        }


        /// <summary>
        /// Refactor this method to go through proper layers and perform soft deletion of an employee to the DB.
        /// </summary>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = _context.Employee.FirstOrDefault(j => j.Id == id);
            if (employee == null) return NotFound();
            employee.IsDeleted = true;

            _context.SaveChanges();
            return Ok(id);
        }



        /// <summary>
        /// Refactor this method to go through proper layers and use Factory pattern
        /// </summary>
        /// <param name="id"></param>
        /// <param name="absentDays"></param>
        /// <param name="workedDays"></param>
        /// <returns></returns>
        [HttpPost("{id}/calculate")]
        public async Task<IActionResult> Calculate(int id, [FromBody] CalculateDto input)
        {
            var result = await Task.FromResult(_context.Employee.FirstOrDefault(m => m.Id == id));
            if (result == null) return NotFound();
            var type = (EmployeeType)result.EmployeeTypeId;

            return type switch
            {
                EmployeeType.Regular =>
                    //create computation for regular.
                    Ok(CalculateRegular(input.AbsentDays)),
                EmployeeType.Contractual =>
                    //create computation for contractual.
                    Ok(CalculateContractual(input.WorkedDays)),
                _ => NotFound("Employee Type not found")
            };

        }

        private double CalculateRegular(double absentDays)
        {
            var regularSalary = 20000;
            var tax = regularSalary * .12;
            var absences = absentDays;
            var perDay = (regularSalary - tax) / 22;
            var totalDeduction = perDay * absences;

            return regularSalary - totalDeduction;
        }

        private double CalculateContractual(double workedDays)
        {
            return 500 * workedDays;
        }

    }
}
