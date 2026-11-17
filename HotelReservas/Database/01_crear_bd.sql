-- Base de datos del caso de uso "Generar reserva de habitación"
IF DB_ID('HotelDB') IS NULL CREATE DATABASE HotelDB;
GO
USE HotelDB;
GO

IF OBJECT_ID('DetalleReserva') IS NOT NULL DROP TABLE DetalleReserva;
IF OBJECT_ID('Reserva')        IS NOT NULL DROP TABLE Reserva;
IF OBJECT_ID('Habitacion')     IS NOT NULL DROP TABLE Habitacion;
IF OBJECT_ID('Cliente')        IS NOT NULL DROP TABLE Cliente;
GO

CREATE TABLE Cliente (
    CodCliente VARCHAR(10)  NOT NULL PRIMARY KEY,
    Nombres    VARCHAR(80)  NOT NULL,
    Apellidos  VARCHAR(80)  NOT NULL DEFAULT ''
);

CREATE TABLE Habitacion (
    NumHabitacion VARCHAR(10)   NOT NULL PRIMARY KEY,
    Tipo          VARCHAR(40)   NOT NULL,
    CostoDia      DECIMAL(10,2) NOT NULL,
    Estado        VARCHAR(12)   NOT NULL DEFAULT 'Disponible'
        CHECK (Estado IN ('Disponible', 'Reservado', 'Ocupado'))
);

CREATE TABLE Reserva (
    NroReserva   INT           NOT NULL PRIMARY KEY,
    CodCliente   VARCHAR(10)   NOT NULL REFERENCES Cliente(CodCliente),
    FechaLlegada DATE          NOT NULL,
    FechaSalida  DATE          NOT NULL,
    CantidadDias INT           NOT NULL,
    SubTotal     DECIMAL(10,2) NOT NULL,
    IGV          DECIMAL(10,2) NOT NULL,
    MontoTotal   DECIMAL(10,2) NOT NULL
);

CREATE TABLE DetalleReserva (
    NroReserva    INT           NOT NULL REFERENCES Reserva(NroReserva),
    NumHabitacion VARCHAR(10)   NOT NULL REFERENCES Habitacion(NumHabitacion),
    NombreHuesped VARCHAR(120)  NOT NULL,
    CostoDia      DECIMAL(10,2) NOT NULL,
    Dias          INT           NOT NULL,
    MontoPagar    DECIMAL(10,2) NOT NULL,
    PRIMARY KEY (NroReserva, NumHabitacion)
);
GO

-- Datos de prueba
INSERT INTO Cliente (CodCliente, Nombres, Apellidos) VALUES
 ('C001', 'Ana',   'Torres Rojas'),
 ('C002', 'Luis',  'Paredes Vega'),
 ('C003', 'María', 'Quispe Salas');

INSERT INTO Habitacion (NumHabitacion, Tipo, CostoDia, Estado) VALUES
 ('101', 'Simple',       120.00, 'Disponible'),
 ('102', 'Simple',       120.00, 'Disponible'),
 ('103', 'Doble',        180.00, 'Disponible'),
 ('201', 'Doble',        180.00, 'Ocupado'),
 ('202', 'Matrimonial',  220.00, 'Disponible'),
 ('301', 'Suite',        350.00, 'Disponible');
GO
