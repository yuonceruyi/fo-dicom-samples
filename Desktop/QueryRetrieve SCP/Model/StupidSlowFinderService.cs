﻿using Dicom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace QueryRetrieve_SCP.Model
{
    public class StupidSlowFinderService : IDicomImageFinderService
    {
        private static string StoragePath = @".\DICOM";


        public List<string> FindPatientFiles(string PatientName, string PatientId)
        {
            // usually here a SQL statement is built to query a Patient-table
            return SearchInFilesystem(
                dcmFile => dcmFile.Get(DicomTag.PatientID, string.Empty),
                dcmFile =>
                {
                    bool matches = true;
                    matches &= MatchFilter(PatientName, dcmFile.Get(DicomTag.PatientName, string.Empty));
                    matches &= MatchFilter(PatientId, dcmFile.Get(DicomTag.PatientID, string.Empty));
                    return matches;
                });
        }


        public List<string> FindStudyFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID)
        {
            // usually here a SQL statement is built to query a Study-table
            return SearchInFilesystem(
                dcmFile => dcmFile.Get(DicomTag.StudyInstanceUID, string.Empty),
                dcmFile =>
                {
                    bool matches = true;
                    matches &= MatchFilter(PatientName, dcmFile.Get(DicomTag.PatientName, string.Empty));
                    matches &= MatchFilter(PatientId, dcmFile.Get(DicomTag.PatientID, string.Empty));
                    matches &= MatchFilter(AccessionNbr, dcmFile.Get(DicomTag.AccessionNumber, string.Empty));
                    matches &= MatchFilter(StudyUID, dcmFile.Get(DicomTag.StudyInstanceUID, string.Empty));
                    return matches;
                });
        }


        public List<string> FindSeriesFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID, string SeriesUID, string Modality)
        {
            // usually here a SQL statement is built to query a Series-table
            return SearchInFilesystem(
                dcmFile => dcmFile.Get(DicomTag.SeriesInstanceUID, string.Empty),
                dcmFile =>
                {
                    bool matches = true;
                    matches &= MatchFilter(PatientName, dcmFile.Get(DicomTag.PatientName, string.Empty));
                    matches &= MatchFilter(PatientId, dcmFile.Get(DicomTag.PatientID, string.Empty));
                    matches &= MatchFilter(AccessionNbr, dcmFile.Get(DicomTag.AccessionNumber, string.Empty));
                    matches &= MatchFilter(StudyUID, dcmFile.Get(DicomTag.StudyInstanceUID, string.Empty));
                    matches &= MatchFilter(SeriesUID, dcmFile.Get(DicomTag.SeriesInstanceUID, string.Empty));
                    matches &= MatchFilter(Modality, dcmFile.Get(DicomTag.Modality, string.Empty));
                    return matches;
                });
        }


        private List<string> SearchInFilesystem(Func<DicomDataset, string> level, Func<DicomDataset, bool> matches)
        {
            string dicomRootDirectory = StoragePath;
            var allFilesOnHarddisk = Directory.GetFiles(dicomRootDirectory, "*.dcm", SearchOption.AllDirectories);
            var matchingFiles = new List<string>(); // holds the file matching the criteria. one representative file per key
            var foundKeys = new List<string>(); // holds the list of keys that have already been found so that only one file per key is returned

            foreach (string fileNameToTest in allFilesOnHarddisk)
            {
                try
                {
                    var dcmFile = DicomFile.Open(fileNameToTest);

                    var key = level(dcmFile.Dataset);
                    if (!string.IsNullOrEmpty(key) && !foundKeys.Contains(key))
                    {
                        if (matches(dcmFile.Dataset))
                        {
                            matchingFiles.Add(fileNameToTest);
                            foundKeys.Add(key);
                        }
                    }
                }
                catch (Exception)
                {
                    // invalid file, ignore here
                }
            }
            return matchingFiles;
        }


        public List<string> FindFilesByUID(string PatientId, string StudyUID, string SeriesUID)
        {
            // normally here a SQL query is constructed. But this implementation searches in file system
            string dicomRootDirectory = StoragePath;
            var allFilesOnHarddisk = Directory.GetFiles(dicomRootDirectory, "*.dcm", SearchOption.AllDirectories);
            var matchingFiles = new List<string>();

            foreach (string fileNameToTest in allFilesOnHarddisk)
            {
                try
                {
                    var dcmFile = DicomFile.Open(fileNameToTest);

                    bool matches = true;
                    matches &= MatchFilter(PatientId, dcmFile.Dataset.Get(DicomTag.PatientID, string.Empty));
                    matches &= MatchFilter(StudyUID, dcmFile.Dataset.Get(DicomTag.StudyInstanceUID, string.Empty));
                    matches &= MatchFilter(SeriesUID, dcmFile.Dataset.Get(DicomTag.SeriesInstanceUID, string.Empty));

                    if (matches)
                    {
                        matchingFiles.Add(fileNameToTest);
                    }
                }
                catch (Exception)
                {
                    // invalid file, ignore here
                }
            }
            return matchingFiles;
        }


        private bool MatchFilter(string filterValue, string valueToTest)
        {
            if (string.IsNullOrEmpty(filterValue))
            {
                // if the QR SCU sends an empty tag, then no filtering should happen
                return true;
            }
            // take into account, that strings may contain a *-wildcard
            var filterRegex = "^" + Regex.Escape(filterValue).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(valueToTest, filterRegex, RegexOptions.IgnoreCase);
        }


    }
}
