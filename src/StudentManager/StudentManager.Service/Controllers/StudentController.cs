﻿using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using StudentManager.Logic.Wrappers;
using StudentManager.Logic.Wrappers.Implementations;
using StudentManager.Service.Models.Receive;
using StudentManager.Service.Models.Subjects;
using StudentManager.Service.Models.Users;
using StudentManager.Tables.Models;

namespace StudentManager.Service.Controllers;

[Route("student")]
[ApiController]
public class StudentController : ExtendedMappingController
{
    private readonly StudentsTableWrapper _students;
    private readonly SubjectsTableWrapper _subjects;
    private readonly TeachersTableWrapper _teachers;
    private readonly PracticeSubgroupsTableWrapper _subgroups;
    private readonly StatementsTableWrapper _statements;
    private readonly IGradesEditorWrapper _gradesEditor;

    public StudentController(IMapper mapper,
        StudentsTableWrapper students,
        SubjectsTableWrapper subjects,
        TeachersTableWrapper teachers,
        PracticeSubgroupsTableWrapper subgroups,
        StatementsTableWrapper statements,
        IGradesEditorWrapper gradesEditor)
        : base(mapper)
    {
        _students = students;
        _subjects = subjects;
        _teachers = teachers;
        _subgroups = subgroups;
        _statements = statements;
        _gradesEditor = gradesEditor;
    }

    [HttpGet("{telegramId}")]
    public async Task<ActionResult<StudentDto>> GetUserByTelegramId(string telegramId)
    {
        var user = await _students.ReadByTelegramId(telegramId);
        if (user.IsFailed)
            return CreateFailResult(user.Errors, HttpStatusCode.NotFound);

        return Mapper.Map<StudentDto>(user.Value);
    }

    [HttpPost("{telegramId}/subjects/total")]
    public async Task<ActionResult<SubjectInfoDto[]>> GetTotalGradesOfSubjects(string telegramId,
        [FromBody] SpreadsheetCreateDto dto)
    {
        var user = await _teachers.ReadByTelegramId(telegramId);
        if (user.IsFailed)
            return CreateFailResult(user.Errors, HttpStatusCode.NotFound);

        var userGrades = await _gradesEditor.ReadByUserId(user.Value.Id);
        if (userGrades.IsFailed)
            return NotFound("CANT_FOUND_USER_GRADES");

        var result =
            await _gradesEditor.WriteToSpreadsheet(dto.LinkToSpreadsheet,
                new List<StudentGratesData> { userGrades.Value });
        return CreateResponseByResult(result);
    }

    [HttpGet("{telegramId}/subjects")]
    public async Task<ActionResult<SubjectInfoDto[]>> GetSubjectsNamesOfUserByTelegramId(string telegramId)
    {
        var user = await _students.ReadByTelegramId(telegramId);
        if (user.IsFailed)
            return CreateFailResult(user.Errors, HttpStatusCode.NotFound);

        var subjects = await _subjects.ReadByGroupId(user.Value.IdGroup);
        var grades = await _gradesEditor.ReadByUserId(user.Value.Id);
        if (grades.IsFailed)
            return CreateFailResult(grades.Errors);

        var subgroups = grades.Value.Subgroups;
        var subjectFiltered = subjects.Value
            .Where(data => subgroups.Count == 0
                           || subgroups.Exists(x => x.SubjectId == data.Id));
        return Mapper.Map<SubjectInfoDto[]>(subjectFiltered);
    }

    [HttpGet("{telegramId}/subject/{subjectId}")]
    public async Task<ActionResult<UserSubjectInfoDto>> GetSubjectInfoOfUserByTelegramId(string telegramId, string subjectId)
    {
        var user = await _students.ReadByTelegramId(telegramId);
        if (user.IsFailed)
            return CreateFailResult(user.Errors, HttpStatusCode.NotFound);

        var subject = await _subjects.ReadById(subjectId);
        if (subject.IsFailed)
            return CreateFailResult(subject.Errors, HttpStatusCode.NotFound);

        var userGradesInfo = await _gradesEditor.ReadByUserId(user.Value.Id);
        if (userGradesInfo.IsFailed)
            return CreateFailResult(userGradesInfo.Errors);

        var statementsBySubject = await _statements.ReadBySubjectId(subject.Value.Id);

        PracticeSubgroupDto? subgroupDto = null;
        string? practiceStatement = null;
        var subgroupInfo = userGradesInfo.Value.Subgroups
            .FirstOrDefault(x => x.SubjectId == subject.Value.Id && x.SubgroupId is not null);
        if (subgroupInfo is { SubgroupId: { } })
        {
            var subgroup = await _subgroups.ReadById(subgroupInfo.SubgroupId);
            if (subgroup.IsFailed)
                return CreateFailResult(subgroup.Errors);

            var practice = await _teachers.ReadById(subgroup.Value.IdTeacher);
            subgroupDto = Mapper.Map<PracticeSubgroupDto>(subgroup.Value);
            subgroupDto.Teacher = Mapper.Map<TeacherDto>(practice.Value);

            var subSheetData = statementsBySubject.ValueOrDefault?
                .FirstOrDefault(x => x.IdSubgroup == subgroup.Value.Id);
            if (subSheetData is not null)
                practiceStatement = CreateGoogleTablesUrl(subSheetData);
        }

        string? lectorStatement = null;
        var lector = await _teachers.ReadById(subject.Value.IdTeacher);
        var sheetData = statementsBySubject.ValueOrDefault?
            .FirstOrDefault(x => x.StatementType == StatementType.Lecture);
        if (sheetData is not null)
            lectorStatement = CreateGoogleTablesUrl(sheetData);

        var subjectDto = Mapper.Map<SubjectDto>(subject.Value);
        subjectDto.Lecturer = Mapper.Map<TeacherDto>(lector.Value);

        return Ok(new UserSubjectInfoDto(subjectDto, subgroupDto, lectorStatement, practiceStatement));
    }

    [HttpGet("{telegramId}/subject/{subjectId}/grades")]
    public async Task<ActionResult<SubjectGradesDto>> GetGradesOfUserByTelegramId(string telegramId, string subjectId)
    {
        var user = await _students.ReadByTelegramId(telegramId);
        if (user.IsFailed)
            return CreateFailResult(user.Errors, HttpStatusCode.NotFound);

        var subjects = await _subjects.ReadByGroupId(user.Value.IdGroup);
        if (subjects.IsFailed)
            return CreateFailResult(subjects.Errors, HttpStatusCode.NotFound);

        var subject = subjects.Value.FirstOrDefault(x => x.Id == subjectId);
        if (subject == null)
            return BadRequest("SUBJECT_IS_NOT_FOR_USER");

        var userGradesInfo = await _gradesEditor.ReadByUserId(user.Value.Id);
        if (userGradesInfo.IsFailed)
            return CreateFailResult(userGradesInfo.Errors);

        var subgroupsGrades = userGradesInfo.Value.Subgroups;

        var grades = subgroupsGrades.Where(x => x.SubjectId == subject.Id).DistinctBy(x => x.SubgroupId).ToArray();
        if (grades.Length == 0)
            return NotFound("CANT_FOUND_USER_GRADES");

        var statementsBySubject = await _statements.ReadBySubjectId(subject.Id);
        if (statementsBySubject.IsFailed)
            return CreateFailResult(statementsBySubject.Errors);

        string? lectorStatement = null;
        var sheetData = statementsBySubject.ValueOrDefault?
            .FirstOrDefault(x => x.StatementType == StatementType.Lecture);
        if (sheetData is not null)
            lectorStatement = CreateGoogleTablesUrl(sheetData);

        string? practiceStatement = null;
        var subgroupId = grades.FirstOrDefault(x => x.SubgroupId is not null)?.SubgroupId;
        if (subgroupId is not null)
        {
            var subSheetData = statementsBySubject.ValueOrDefault?
                .FirstOrDefault(x => x.IdSubgroup == subgroupId);
            if (subSheetData is not null)
                practiceStatement = CreateGoogleTablesUrl(subSheetData);
        }

        var mappedGrades = grades.Select(x => Mapper.Map<SubjectGradeDto>(x.SubjectGrate)).ToArray();
        return new SubjectGradesDto(subject.Title, mappedGrades, lectorStatement, practiceStatement);
    }

    private static string CreateGoogleTablesUrl(StatementSheetData data) =>
        $"https://docs.google.com/spreadsheets/d/{data.SpreadsheetId}/edit#gid={data.SheetId}";
}