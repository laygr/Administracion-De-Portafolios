module FDatesM
open System

let yearsBetweenDates (d1:DateTime) (d2:DateTime) = (d2 - d1).TotalDays / 365.0

let percentageOfTimeOccurred (d0:DateTime) (dt:DateTime) (dT:DateTime) =
    (dt - d0).TotalDays / (dT - d0).TotalDays