CREATE OR ALTER PROCEDURE dbo.usp_PruebaFormatter
    @id_cliente INT
AS
BEGIN
SET NOCOUNT ON;
select top 10 a.id_cliente,a.nombre,case when a.activo=1 then 'ACTIVO' else 'INACTIVO' end as estado_texto from dbo.tbl_cliente a inner join dbo.tbl_estado b on b.id_estado=a.id_estado where a.id_cliente=@id_cliente and (a.id_estado=2 or a.id_estado=3) order by a.nombre;
insert into dbo.tbl_destino(id_cliente,nombre,estado) select a.id_cliente,a.nombre,1 from dbo.tbl_cliente a where a.activo=1;
update dbo.tbl_destino set nombre='PRUEBA',estado=2 where id_cliente=@id_cliente;
BEGIN TRY
BEGIN TRANSACTION;
delete from dbo.tbl_temporal where id_cliente=@id_cliente;
COMMIT;
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK;
THROW;
END CATCH
END
